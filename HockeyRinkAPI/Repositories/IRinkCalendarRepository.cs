using HockeyRinkAPI.Models;

namespace HockeyRinkAPI.Repositories;

/// <summary>
/// Read-only repository for rink calendar queries.
/// Retrieves sessions, games, and blockouts for a given rink and date range.
/// </summary>
public interface IRinkCalendarRepository
{
    /// <summary>Returns all sessions booked on a rink for a given date.</summary>
    Task<List<Session>> GetSessionsForRinkOnDateAsync(int rinkId, DateTime date);

    /// <summary>Returns all games scheduled on a rink for a given date.</summary>
    Task<List<Game>> GetGamesForRinkOnDateAsync(int rinkId, DateTime date);

    /// <summary>Returns all blockouts for a rink on a given date.</summary>
    Task<List<RinkBlockout>> GetBlockoutsForRinkOnDateAsync(int rinkId, DateTime date);

    /// <summary>
    /// Returns the count of bookings (sessions + games + blockouts) per day
    /// for a rink in the given month. Used for calendar badge indicators.
    /// </summary>
    Task<Dictionary<int, int>> GetMonthBookingCountsAsync(int rinkId, int year, int month);

    /// <summary>Returns sessions that overlap a proposed time range on a rink.</summary>
    Task<List<Session>> GetOverlappingSessionsAsync(int rinkId, DateTime start, DateTime end, int? excludeGameId = null);

    /// <summary>Returns games that overlap a proposed time range on a rink (including 10-min buffer).</summary>
    Task<List<Game>> GetOverlappingGamesAsync(int rinkId, DateTime start, DateTime end, int? excludeGameId = null);

    /// <summary>Returns blockouts that overlap a proposed time range on a rink.</summary>
    Task<List<RinkBlockout>> GetOverlappingBlockoutsAsync(int rinkId, DateTime start, DateTime end);
}
