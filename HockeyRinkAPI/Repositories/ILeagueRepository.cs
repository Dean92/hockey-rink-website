using HockeyRinkAPI.Models;

namespace HockeyRinkAPI.Repositories;

public interface ILeagueRepository
{
    Task<List<League>> GetAllAsync();
    Task<List<League>> GetAllWithTeamsAsync();
    Task<League?> GetByIdAsync(int id);
    Task SaveChangesAsync();
}
