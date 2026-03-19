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

    public Task SaveChangesAsync() =>
        _dbContext.SaveChangesAsync();
}
