using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HockeyRinkAPI.Repositories;

public class RinkCalendarRepository : IRinkCalendarRepository
{
    private readonly AppDbContext _dbContext;

    public RinkCalendarRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Session>> GetSessionsForRinkOnDateAsync(int rinkId, DateTime date)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        return _dbContext.Sessions
            .Include(s => s.League)
            .Where(s => s.RinkId == rinkId
                && s.StartDate < dayEnd
                && s.EndDate >= dayStart)
            .OrderBy(s => s.StartDate)
            .ToListAsync();
    }

    public Task<List<Game>> GetGamesForRinkOnDateAsync(int rinkId, DateTime date)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        return _dbContext.Games
            .Include(g => g.HomeTeam)
            .Include(g => g.AwayTeam)
            .Include(g => g.Session).ThenInclude(s => s.League)
            .Where(g => g.RinkId == rinkId
                && g.GameDate >= dayStart
                && g.GameDate < dayEnd
                && g.Status != "Cancelled")
            .OrderBy(g => g.GameDate)
            .ToListAsync();
    }

    public Task<List<RinkBlockout>> GetBlockoutsForRinkOnDateAsync(int rinkId, DateTime date)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        return _dbContext.RinkBlockouts
            .Where(rb => rb.RinkId == rinkId
                && rb.StartDateTime < dayEnd
                && rb.EndDateTime > dayStart)
            .OrderBy(rb => rb.StartDateTime)
            .ToListAsync();
    }

    public async Task<Dictionary<int, int>> GetMonthBookingCountsAsync(int rinkId, int year, int month)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var sessionDays = await _dbContext.Sessions
            .Where(s => s.RinkId == rinkId
                && s.StartDate < monthEnd
                && s.EndDate >= monthStart)
            .Select(s => s.StartDate.Day)
            .ToListAsync();

        var gameDays = await _dbContext.Games
            .Where(g => g.RinkId == rinkId
                && g.GameDate >= monthStart
                && g.GameDate < monthEnd
                && g.Status != "Cancelled")
            .Select(g => g.GameDate.Day)
            .ToListAsync();

        var blockoutDays = await _dbContext.RinkBlockouts
            .Where(rb => rb.RinkId == rinkId
                && rb.StartDateTime < monthEnd
                && rb.EndDateTime > monthStart)
            .Select(rb => rb.StartDateTime.Day)
            .ToListAsync();

        var counts = new Dictionary<int, int>();
        foreach (var day in sessionDays.Concat(gameDays).Concat(blockoutDays))
        {
            counts.TryGetValue(day, out var current);
            counts[day] = current + 1;
        }

        return counts;
    }

    public Task<List<Session>> GetOverlappingSessionsAsync(int rinkId, DateTime start, DateTime end, int? excludeGameId = null)
    {
        return _dbContext.Sessions
            .Where(s => s.RinkId == rinkId
                && s.StartDate < end
                && s.EndDate > start)
            .ToListAsync();
    }

    public Task<List<Game>> GetOverlappingGamesAsync(int rinkId, DateTime start, DateTime end, int? excludeGameId = null)
    {
        // Apply 10-minute buffer: a game occupies its slot + 10 minutes after
        var bufferEnd = end.AddMinutes(10);

        return _dbContext.Games
            .Include(g => g.HomeTeam)
            .Include(g => g.AwayTeam)
            .Where(g => g.RinkId == rinkId
                && g.Status != "Cancelled"
                && (excludeGameId == null || g.Id != excludeGameId))
            .ToListAsync();
    }

    public Task<List<RinkBlockout>> GetOverlappingBlockoutsAsync(int rinkId, DateTime start, DateTime end)
    {
        return _dbContext.RinkBlockouts
            .Where(rb => rb.RinkId == rinkId
                && rb.StartDateTime < end
                && rb.EndDateTime > start)
            .ToListAsync();
    }
}
