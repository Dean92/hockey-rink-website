using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HockeyRinkAPI.Repositories;

public class RinkRepository : IRinkRepository
{
    private readonly AppDbContext _dbContext;

    public RinkRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Rink>> GetAllAsync() =>
        _dbContext.Rinks
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync();

    public async Task<Rink?> GetByIdAsync(int id) =>
        await _dbContext.Rinks.FindAsync(id);

    public Task<Rink?> GetByIdWithBlockoutsAsync(int id) =>
        _dbContext.Rinks
            .Include(r => r.Blockouts)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task AddAsync(Rink rink) =>
        await _dbContext.Rinks.AddAsync(rink);

    public Task DeactivateAsync(Rink rink)
    {
        rink.IsActive = false;
        return Task.CompletedTask;
    }

    public Task<List<RinkBlockout>> GetBlockoutsAsync(int rinkId) =>
        _dbContext.RinkBlockouts
            .Where(rb => rb.RinkId == rinkId)
            .OrderBy(rb => rb.StartDateTime)
            .ToListAsync();

    public async Task AddBlockoutAsync(RinkBlockout blockout) =>
        await _dbContext.RinkBlockouts.AddAsync(blockout);

    public async Task<RinkBlockout?> GetBlockoutByIdAsync(int id) =>
        await _dbContext.RinkBlockouts.FindAsync(id);

    public Task DeleteBlockoutAsync(RinkBlockout blockout)
    {
        _dbContext.RinkBlockouts.Remove(blockout);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() =>
        _dbContext.SaveChangesAsync();
}
