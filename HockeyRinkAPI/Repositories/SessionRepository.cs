using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HockeyRinkAPI.Repositories;

public class SessionRepository : ISessionRepository
{
    private readonly AppDbContext _dbContext;

    public SessionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Session>> GetAllWithDetailsAsync() =>
        _dbContext.Sessions
            .Include(s => s.League)
            .Include(s => s.SessionRegistrations)
            .ToListAsync();

    public async Task<List<Session>> GetFilteredAsync(int? leagueId, DateTime? date)
    {
        var query = _dbContext.Sessions
            .Include(s => s.League)
            .Include(s => s.SessionRegistrations)
            .AsQueryable();

        if (leagueId.HasValue)
            query = query.Where(s => s.LeagueId == leagueId.Value);

        if (date.HasValue)
            query = query.Where(s => s.StartDate.Date == date.Value.Date);

        return await query.ToListAsync();
    }

    public async Task<Session?> GetByIdAsync(int id) =>
        await _dbContext.Sessions.FindAsync(id);

    public Task<Session?> GetByIdWithRegistrationsAsync(int id) =>
        _dbContext.Sessions
            .Include(s => s.SessionRegistrations)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(s => s.Id == id);

    public Task<Session?> GetByIdWithLeagueAsync(int id) =>
        _dbContext.Sessions
            .Include(s => s.League)
            .FirstOrDefaultAsync(s => s.Id == id);

    public Task<List<Session>> GetActiveSessionsWithDetailsAsync(DateTime now) =>
        _dbContext.Sessions
            .Include(s => s.League)
            .Include(s => s.SessionRegistrations)
            .Where(s => s.EndDate >= now)
            .OrderBy(s => s.StartDate)
            .ToListAsync();

    public Task<List<Session>> GetUpcomingAsync(DateTime from, DateTime to) =>
        _dbContext.Sessions
            .Include(s => s.League)
            .Include(s => s.SessionRegistrations)
            .Where(s => s.StartDate >= from && s.StartDate <= to)
            .OrderBy(s => s.StartDate)
            .ToListAsync();

    public async Task AddAsync(Session session) =>
        await _dbContext.Sessions.AddAsync(session);

    public void Remove(Session session) =>
        _dbContext.Sessions.Remove(session);

    public Task SaveChangesAsync() =>
        _dbContext.SaveChangesAsync();
}
