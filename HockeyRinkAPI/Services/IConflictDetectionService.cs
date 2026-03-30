using HockeyRinkAPI.Models.Responses;

namespace HockeyRinkAPI.Services;

/// <summary>
/// Checks whether a proposed rink time slot conflicts with existing bookings.
/// </summary>
public interface IConflictDetectionService
{
    /// <summary>
    /// Returns a ConflictCheckResponse indicating whether the proposed slot
    /// overlaps any session, game (+ 10-min buffer), or blockout on the given rink.
    /// </summary>
    Task<ConflictCheckResponse> CheckAsync(int rinkId, DateTime start, DateTime end, int? excludeGameId = null);
}
