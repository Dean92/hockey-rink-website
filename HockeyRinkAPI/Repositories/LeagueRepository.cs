using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HockeyRinkAPI.Repositories;

public class LeagueRepository : ILeagueRepository
{
    private readonly AppDbContext _dbContext;

    public LeagueRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<League>> GetAllAsync() =>
        _dbContext.Leagues.ToListAsync();

    public Task<List<League>> GetAllWithTeamsAsync() =>
        _dbContext.Leagues
            .Include(l => l.Teams)
            .OrderBy(l => l.Name)
            .ToListAsync();

    public async Task<League?> GetByIdAsync(int id) =>
        await _dbContext.Leagues.FindAsync(id);

    public async Task<League?> GetByIdWithTeamsAsync(int id) =>
        await _dbContext.Leagues.Include(l => l.Teams).FirstOrDefaultAsync(l => l.Id == id);

    public async Task AddAsync(League league)
    {
        await _dbContext.Leagues.AddAsync(league);
    }

    public Task DeleteAsync(League league)
    {
        _dbContext.Leagues.Remove(league);
        return Task.CompletedTask;
    }

    public async Task NullifySessionLeagueIdAsync(int leagueId)
    {
        var sessions = await _dbContext.Sessions
            .Where(s => s.LeagueId == leagueId)
            .ToListAsync();

        foreach (var session in sessions)
            session.LeagueId = null;
    }

    public Task SaveChangesAsync() =>
        _dbContext.SaveChangesAsync();
}
