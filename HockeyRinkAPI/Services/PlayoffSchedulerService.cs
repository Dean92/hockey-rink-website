using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using HockeyRinkAPI.Models.Requests;
using HockeyRinkAPI.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace HockeyRinkAPI.Services;

/// <summary>
/// Generates a single-elimination playoff bracket preview.
/// Follows Single-Responsibility: seeding, bracket building, and slot assignment
/// are separate private methods. No database writes.
/// </summary>
public class PlayoffSchedulerService : IPlayoffSchedulerService
{
    private readonly AppDbContext _db;
    private readonly IConflictDetectionService _conflictService;

    public PlayoffSchedulerService(AppDbContext db, IConflictDetectionService conflictService)
    {
        _db = db;
        _conflictService = conflictService;
    }

    public async Task<GenerateScheduleResponse> GenerateAsync(GeneratePlayoffRequest req)
    {
        // 1. Load teams for the session
        var teams = await _db.Teams
            .Where(t => t.SessionId == req.SessionId)
            .OrderBy(t => t.TeamName)
            .ToListAsync();

        if (teams.Count < 2)
            return EmptyResponse("Session has fewer than 2 teams — nothing to schedule");

        // 2. Seed teams by regular-season W-L record
        var seededTeams = await SeedTeamsAsync(req.SessionId, teams);

        // 3. Build bracket matchups round by round (byes for non-power-of-2 counts)
        var rounds = BuildBracketRounds(seededTeams);

        // 4. Build available slots
        var excludedDates = BuildExcludedDates(req);
        var slots = BuildAvailableSlots(req, excludedDates, out var skippedDates);

        // 5. Assign each round's games to slots (one round must finish before the next)
        var proposed = new List<ProposedGameDto>();
        var unscheduled = new List<UnscheduledMatchupDto>();

        int slotIndex = 0;
        var gamesThisNight = new Dictionary<DateTime, int>();

        foreach (var round in rounds)
        {
            foreach (var (home, away) in round)
            {
                bool scheduled = false;

                while (slotIndex < slots.Count)
                {
                    var slot = slots[slotIndex];
                    var slotDate = slot.Date;

                    gamesThisNight.TryGetValue(slotDate, out var nightCount);
                    if (nightCount >= req.GamesPerNight)
                    {
                        slotIndex = slots.FindIndex(slotIndex + 1, s => s.Date != slotDate);
                        if (slotIndex < 0) break;
                        continue;
                    }

                    var slotEnd = slot.AddMinutes(req.GameLengthMinutes);
                    var conflict = await _conflictService.CheckAsync(req.RinkId, slot, slotEnd);

                    slotIndex++;

                    if (conflict.HasConflict) continue;

                    proposed.Add(new ProposedGameDto
                    {
                        GameDate = slot,
                        HomeTeamId = home.Id,
                        HomeTeamName = home.TeamName,
                        AwayTeamId = away.Id,
                        AwayTeamName = away.TeamName,
                        RinkId = req.RinkId,
                        HasConflict = false,
                        GameType = "Playoff"
                    });

                    gamesThisNight[slotDate] = nightCount + 1;
                    scheduled = true;
                    break;
                }

                if (!scheduled)
                    unscheduled.Add(new UnscheduledMatchupDto
                    {
                        HomeTeamId = home.Id,
                        HomeTeamName = home.TeamName,
                        AwayTeamId = away.Id,
                        AwayTeamName = away.TeamName,
                        Reason = "No available slot found in the specified date range"
                    });
            }
        }

        return new GenerateScheduleResponse
        {
            ProposedGames = proposed,
            SkippedDates = skippedDates,
            UnscheduledMatchups = unscheduled,
            TotalGamesGenerated = proposed.Count
        };
    }

    // ── Seeding ───────────────────────────────────────────────────────────────

    private async Task<List<Team>> SeedTeamsAsync(int sessionId, List<Team> teams)
    {
        // Count wins from completed regular-season games only
        var completedGames = await _db.Games
            .Where(g => g.SessionId == sessionId
                     && g.GameType == "RegularSeason"
                     && g.Status == "Completed"
                     && g.HomeScore.HasValue
                     && g.AwayScore.HasValue)
            .ToListAsync();

        var wins = new Dictionary<int, int>();
        foreach (var t in teams) wins[t.Id] = 0;

        foreach (var g in completedGames)
        {
            if (g.HomeScore > g.AwayScore) wins[g.HomeTeamId]++;
            else if (g.AwayScore > g.HomeScore) wins[g.AwayTeamId]++;
        }

        // Sort descending by wins, then alphabetically for ties
        return teams
            .OrderByDescending(t => wins.GetValueOrDefault(t.Id, 0))
            .ThenBy(t => t.TeamName)
            .ToList();
    }

    // ── Bracket Building ──────────────────────────────────────────────────────

    /// <summary>
    /// Builds rounds for a single-elimination bracket.
    /// Byes: top seeds get first-round byes so the second round is a clean power-of-2.
    /// Seeding: 1 vs N, 2 vs N-1, etc. (standard bracket).
    /// Returns Round 1 matchups only — subsequent rounds depend on game results
    /// and must be generated after each round is complete.
    /// </summary>
    private static List<List<(Team home, Team away)>> BuildBracketRounds(List<Team> seeded)
    {
        int n = seeded.Count;
        int bracketSize = NextPowerOfTwo(n);

        // Fill bracket slots top-down; slots beyond seeded count are null (bye)
        var slots = new Team?[bracketSize];
        for (int i = 0; i < n; i++)
            slots[i] = seeded[i];

        // Standard bracket order: interleave so 1v(last), 2v(last-1), etc.
        var bracketOrder = BracketPositions(bracketSize); // 1-based seed indices

        var round1 = new List<(Team, Team)>();
        for (int i = 0; i < bracketOrder.Count; i += 2)
        {
            int seedA = bracketOrder[i];     // 1-based
            int seedB = bracketOrder[i + 1]; // 1-based

            var teamA = seedA <= n ? slots[seedA - 1] : null;
            var teamB = seedB <= n ? slots[seedB - 1] : null;

            // Skip if either side is a bye (top seed advances automatically)
            if (teamA == null || teamB == null) continue;

            round1.Add((teamA, teamB));
        }

        var rounds = new List<List<(Team, Team)>>();
        if (round1.Count > 0)
            rounds.Add(round1);

        return rounds;
    }

    /// <summary>
    /// Returns bracket slot positions (1-based seed numbers) in the standard
    /// single-elimination order so that the best seeds meet as late as possible.
    /// For 8 teams: [1, 8, 5, 4, 3, 6, 7, 2] (pairs: 1v8, 5v4, 3v6, 7v2)
    /// </summary>
    private static List<int> BracketPositions(int size)
    {
        if (size == 2) return [1, 2];

        var half = BracketPositions(size / 2);
        var result = new List<int>(size);
        foreach (int pos in half)
        {
            result.Add(pos);
            result.Add(size + 1 - pos);
        }
        return result;
    }

    private static int NextPowerOfTwo(int n)
    {
        int p = 1;
        while (p < n) p <<= 1;
        return p;
    }

    // ── Date / Slot helpers (mirrored from ScheduleGeneratorService) ──────────

    private static HashSet<DateTime> BuildExcludedDates(GeneratePlayoffRequest req)
    {
        var excluded = new HashSet<DateTime>();
        foreach (var ds in req.ExcludeDates)
            if (DateTime.TryParse(ds, out var d)) excluded.Add(d.Date);

        if (req.ExcludeUsHolidays)
        {
            for (int y = req.StartDate.Year; y <= req.EndDate.Year; y++)
                foreach (var h in GetUsHolidays(y)) excluded.Add(h);
        }
        return excluded;
    }

    private static List<DateTime> BuildAvailableSlots(
        GeneratePlayoffRequest req,
        HashSet<DateTime> excluded,
        out List<SkippedDateDto> skippedDates)
    {
        skippedDates = new List<SkippedDateDto>();
        var slots = new List<DateTime>();
        var slotDuration = TimeSpan.FromMinutes(req.GameLengthMinutes + req.BufferMinutes);

        for (var date = req.StartDate.Date; date <= req.EndDate.Date; date = date.AddDays(1))
        {
            if (!req.DaysOfWeek.Contains((int)date.DayOfWeek)) continue;
            if (excluded.Contains(date))
            {
                skippedDates.Add(new SkippedDateDto { Date = date, Reason = "Excluded date" });
                continue;
            }
            var slotTime = date + req.DailyStartTime;
            var latestStart = date + req.DailyEndTime;
            while (slotTime <= latestStart)
            {
                slots.Add(slotTime);
                slotTime += slotDuration;
            }
        }
        return slots;
    }

    private static IEnumerable<DateTime> GetUsHolidays(int year)
    {
        yield return new DateTime(year, 1, 1);
        yield return GetLastMonday(year, 5);
        yield return new DateTime(year, 7, 4);
        yield return GetFirstMonday(year, 9);
        yield return GetNthWeekday(year, 11, DayOfWeek.Thursday, 4);
        yield return new DateTime(year, 12, 25);
    }

    private static DateTime GetLastMonday(int year, int month)
    {
        var d = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        while (d.DayOfWeek != DayOfWeek.Monday) d = d.AddDays(-1);
        return d;
    }

    private static DateTime GetFirstMonday(int year, int month)
    {
        var d = new DateTime(year, month, 1);
        while (d.DayOfWeek != DayOfWeek.Monday) d = d.AddDays(1);
        return d;
    }

    private static DateTime GetNthWeekday(int year, int month, DayOfWeek dow, int n)
    {
        var d = new DateTime(year, month, 1);
        while (d.DayOfWeek != dow) d = d.AddDays(1);
        return d.AddDays(7 * (n - 1));
    }

    private static GenerateScheduleResponse EmptyResponse(string reason) =>
        new()
        {
            UnscheduledMatchups =
            [
                new UnscheduledMatchupDto
                {
                    HomeTeamId = 0, HomeTeamName = "N/A",
                    AwayTeamId = 0, AwayTeamName = "N/A",
                    Reason = reason
                }
            ]
        };
}
