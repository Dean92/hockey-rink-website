using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HockeyRinkAPI.Repositories;

public class GameRepository : IGameRepository
{
    private readonly AppDbContext _dbContext;

    public GameRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Game>> GetLeagueGamesAsync(
        int leagueId,
        int? teamId = null,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? rinkId = null)
    {
        var query = _dbContext.Games
            .Include(g => g.HomeTeam)
            .Include(g => g.AwayTeam)
            .Include(g => g.Rink)
            .Include(g => g.Session)
            .Where(g => g.Session.LeagueId == leagueId)
            .AsQueryable();

        if (teamId.HasValue)
            query = query.Where(g => g.HomeTeamId == teamId || g.AwayTeamId == teamId);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(g => g.Status == status);

        if (startDate.HasValue)
            query = query.Where(g => g.GameDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(g => g.GameDate <= endDate.Value);

        if (rinkId.HasValue)
            query = query.Where(g => g.RinkId == rinkId);

        return query.OrderBy(g => g.GameDate).ToListAsync();
    }

    public Task<Game?> GetByIdAsync(int id)
    {
        return _dbContext.Games
            .Include(g => g.HomeTeam)
            .Include(g => g.AwayTeam)
            .Include(g => g.Rink)
            .Include(g => g.Session)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<Game> UpdateAsync(Game game)
    {
        game.UpdatedAt = DateTime.UtcNow;
        _dbContext.Games.Update(game);
        await _dbContext.SaveChangesAsync();
        return game;
    }

    public async Task CancelAsync(int id)
    {
        var game = await _dbContext.Games.FindAsync(id)
            ?? throw new KeyNotFoundException($"Game {id} not found");

        game.Status = "Cancelled";
        game.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }
}
