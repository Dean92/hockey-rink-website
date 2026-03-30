using HockeyRinkAPI.Models;

namespace HockeyRinkAPI.Repositories;

public interface IRinkRepository
{
    Task<List<Rink>> GetAllAsync();
    Task<Rink?> GetByIdAsync(int id);
    Task<Rink?> GetByIdWithBlockoutsAsync(int id);
    Task AddAsync(Rink rink);
    Task DeactivateAsync(Rink rink);
    Task<List<RinkBlockout>> GetBlockoutsAsync(int rinkId);
    Task AddBlockoutAsync(RinkBlockout blockout);
    Task<RinkBlockout?> GetBlockoutByIdAsync(int id);
    Task DeleteBlockoutAsync(RinkBlockout blockout);
    Task SaveChangesAsync();
}
