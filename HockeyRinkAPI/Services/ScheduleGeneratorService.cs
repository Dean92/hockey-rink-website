using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models.Requests;
using HockeyRinkAPI.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace HockeyRinkAPI.Services;

/// <summary>
/// Generates a round-robin league schedule preview.
/// No database writes — caller decides whether to confirm.
/// </summary>
public class ScheduleGeneratorService : IScheduleGeneratorService
{
    private readonly AppDbContext _db;
    private readonly IConflictDetectionService _conflictService;

    public ScheduleGeneratorService(AppDbContext db, IConflictDetectionService conflictService)
    {
        _db = db;
        _conflictService = conflictService;
    }

    public async Task<GenerateScheduleResponse> GenerateAsync(GenerateScheduleRequest req)
    {
        // 1. Load teams for the session
        var teams = await _db.Teams
            .Where(t => t.SessionId == req.SessionId)
            .OrderBy(t => t.TeamName)
            .ToListAsync();

        if (teams.Count < 2)
            return new GenerateScheduleResponse
            {
                UnscheduledMatchups =
                [
                    new UnscheduledMatchupDto
                    {
                        HomeTeamId = 0, HomeTeamName = "N/A",
                        AwayTeamId = 0, AwayTeamName = "N/A",
                        Reason = "Session has fewer than 2 teams — nothing to schedule"
                    }
                ]
            };

        // 2. Build round-robin matchup list
        var matchups = BuildRoundRobinMatchups(teams, req.GamesPerMatchup);

        // 3. Determine excluded dates
        var excludedDates = BuildExcludedDates(req);

        // 4. Build ordered list of available slots
        var slots = BuildAvailableSlots(req, excludedDates, out var skippedDates);

        // 5. Assign matchups to slots with conflict detection
        var proposed = new List<ProposedGameDto>();
        var unscheduled = new List<UnscheduledMatchupDto>();

        var slotIndex = 0;
        var gamesThisNight = new Dictionary<DateTime, int>(); // date → count

        foreach (var (home, away) in matchups)
        {
            bool scheduled = false;

            while (slotIndex < slots.Count)
            {
                var slot = slots[slotIndex];
                var slotDate = slot.Date;

                // Enforce games-per-night limit on this rink
                gamesThisNight.TryGetValue(slotDate, out var nightCount);
                if (nightCount >= req.GamesPerNight)
                {
                    // Skip to next day
                    slotIndex = slots.FindIndex(slotIndex + 1, s => s.Date != slotDate);
                    if (slotIndex < 0) break;
                    continue;
                }

                var slotEnd = slot.AddMinutes(req.GameLengthMinutes);
                var conflict = await _conflictService.CheckAsync(req.RinkId, slot, slotEnd);

                slotIndex++;

                if (conflict.HasConflict)
                    continue;

                proposed.Add(new ProposedGameDto
                {
                    GameDate = slot,
                    HomeTeamId = home.Id,
                    HomeTeamName = home.TeamName,
                    AwayTeamId = away.Id,
                    AwayTeamName = away.TeamName,
                    RinkId = req.RinkId,
                    HasConflict = false
                });

                gamesThisNight[slotDate] = nightCount + 1;
                scheduled = true;
                break;
            }

            if (!scheduled)
            {
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

    // ── Round Robin ───────────────────────────────────────────────────────────

    /// <summary>
    /// Standard round-robin algorithm (circle/polygon method).
    /// Returns a list of (home, away) matchup pairs, repeated gamesPerMatchup times
    /// with home/away swapped on even repetitions.
    /// </summary>
    private static List<(Models.Team home, Models.Team away)> BuildRoundRobinMatchups(
        List<Models.Team> teams, int gamesPerMatchup)
    {
        var allMatchups = new List<(Models.Team, Models.Team)>();
        var list = new List<Models.Team>(teams);

        // Bye team for odd counts
        if (list.Count % 2 != 0)
            list.Add(null!);

        int n = list.Count;
        int rounds = n - 1;

        for (int rep = 0; rep < gamesPerMatchup; rep++)
        {
            var roundTeams = new List<Models.Team>(list);
            bool swapHomeAway = rep % 2 == 1;

            for (int round = 0; round < rounds; round++)
            {
                for (int i = 0; i < n / 2; i++)
                {
                    var t1 = roundTeams[i];
                    var t2 = roundTeams[n - 1 - i];

                    // Skip bye
                    if (t1 == null || t2 == null) continue;

                    if (swapHomeAway)
                        allMatchups.Add((t2, t1));
                    else
                        allMatchups.Add((t1, t2));
                }

                // Rotate: fix first element, rotate the rest
                var last = roundTeams[n - 1];
                roundTeams.RemoveAt(n - 1);
                roundTeams.Insert(1, last);
            }
        }

        return allMatchups;
    }

    // ── Date helpers ──────────────────────────────────────────────────────────

    private static HashSet<DateTime> BuildExcludedDates(GenerateScheduleRequest req)
    {
        var excluded = new HashSet<DateTime>();

        foreach (var ds in req.ExcludeDates)
        {
            if (DateTime.TryParse(ds, out var d))
                excluded.Add(d.Date);
        }

        if (req.ExcludeUsHolidays)
        {
            var start = req.StartDate.Year;
            var end = req.EndDate.Year;
            for (int y = start; y <= end; y++)
            {
                foreach (var h in GetUsHolidays(y))
                    excluded.Add(h);
            }
        }

        return excluded;
    }

    /// <summary>
    /// Builds ordered DateTime slots (one per game position) for the given date range,
    /// respecting days-of-week, daily time window, and excluded dates.
    /// </summary>
    private static List<DateTime> BuildAvailableSlots(
        GenerateScheduleRequest req,
        HashSet<DateTime> excluded,
        out List<SkippedDateDto> skippedDates)
    {
        skippedDates = new List<SkippedDateDto>();
        var slots = new List<DateTime>();
        var slotDuration = TimeSpan.FromMinutes(req.GameLengthMinutes + req.BufferMinutes);

        for (var date = req.StartDate.Date; date <= req.EndDate.Date; date = date.AddDays(1))
        {
            if (!req.DaysOfWeek.Contains((int)date.DayOfWeek))
                continue;

            if (excluded.Contains(date))
            {
                skippedDates.Add(new SkippedDateDto
                {
                    Date = date,
                    Reason = req.ExcludeUsHolidays && IsUsHoliday(date) ? "US Holiday" : "Excluded date"
                });
                continue;
            }

            // Generate hourly-ish slots within the daily window
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

    // ── US Holidays ───────────────────────────────────────────────────────────

    private static IEnumerable<DateTime> GetUsHolidays(int year)
    {
        yield return new DateTime(year, 1, 1);                          // New Year's Day
        yield return GetLastMonday(year, 5);                            // Memorial Day
        yield return new DateTime(year, 7, 4);                         // Independence Day
        yield return GetFirstMonday(year, 9);                           // Labor Day
        yield return GetNthWeekday(year, 11, DayOfWeek.Thursday, 4);   // Thanksgiving
        yield return new DateTime(year, 12, 25);                        // Christmas
    }

    private static bool IsUsHoliday(DateTime date)
        => GetUsHolidays(date.Year).Any(h => h.Date == date.Date);

    private static DateTime GetLastMonday(int year, int month)
    {
        var last = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        while (last.DayOfWeek != DayOfWeek.Monday) last = last.AddDays(-1);
        return last;
    }

    private static DateTime GetFirstMonday(int year, int month)
    {
        var first = new DateTime(year, month, 1);
        while (first.DayOfWeek != DayOfWeek.Monday) first = first.AddDays(1);
        return first;
    }

    private static DateTime GetNthWeekday(int year, int month, DayOfWeek dow, int n)
    {
        var d = new DateTime(year, month, 1);
        while (d.DayOfWeek != dow) d = d.AddDays(1);
        return d.AddDays(7 * (n - 1));
    }
}
