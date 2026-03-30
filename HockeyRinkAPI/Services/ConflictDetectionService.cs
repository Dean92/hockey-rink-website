using HockeyRinkAPI.Models.Responses;
using HockeyRinkAPI.Repositories;

namespace HockeyRinkAPI.Services;

/// <summary>
/// Detects booking conflicts on a rink for a proposed time slot.
/// Checks sessions, games (with 10-minute post-game buffer), and blockouts.
/// </summary>
public class ConflictDetectionService : IConflictDetectionService
{
    private const int GameBufferMinutes = 10;

    private readonly IRinkCalendarRepository _calendarRepository;

    public ConflictDetectionService(IRinkCalendarRepository calendarRepository)
    {
        _calendarRepository = calendarRepository;
    }

    public async Task<ConflictCheckResponse> CheckAsync(int rinkId, DateTime start, DateTime end, int? excludeGameId = null)
    {
        // Check blockouts first — they take precedence
        var blockouts = await _calendarRepository.GetOverlappingBlockoutsAsync(rinkId, start, end);
        if (blockouts.Count > 0)
        {
            var b = blockouts[0];
            return new ConflictCheckResponse
            {
                HasConflict = true,
                ConflictType = "blockout",
                ConflictTitle = b.Reason ?? "Rink Blockout",
                ConflictStart = b.StartDateTime,
                ConflictEnd = b.EndDateTime
            };
        }

        // Check sessions
        var sessions = await _calendarRepository.GetOverlappingSessionsAsync(rinkId, start, end);
        if (sessions.Count > 0)
        {
            var s = sessions[0];
            return new ConflictCheckResponse
            {
                HasConflict = true,
                ConflictType = "session",
                ConflictTitle = s.Name,
                ConflictStart = s.StartDate,
                ConflictEnd = s.EndDate
            };
        }

        // Check games — each game occupies its slot + 10-minute buffer after
        var games = await _calendarRepository.GetOverlappingGamesAsync(rinkId, start, end, excludeGameId);
        var conflictingGame = games.FirstOrDefault(g =>
        {
            // We don't store game end time — caller provides it via `end`
            // For overlap check we treat proposed slot against existing game + buffer
            var gameEnd = g.GameDate.AddMinutes(
                (end - start).TotalMinutes + GameBufferMinutes
            );
            return g.GameDate < end && gameEnd > start;
        });

        if (conflictingGame != null)
        {
            return new ConflictCheckResponse
            {
                HasConflict = true,
                ConflictType = "game",
                ConflictTitle = $"{conflictingGame.HomeTeam?.TeamName ?? "TBD"} vs {conflictingGame.AwayTeam?.TeamName ?? "TBD"}",
                ConflictStart = conflictingGame.GameDate,
                ConflictEnd = conflictingGame.GameDate.Add(end - start).AddMinutes(GameBufferMinutes)
            };
        }

        return new ConflictCheckResponse { HasConflict = false };
    }
}
