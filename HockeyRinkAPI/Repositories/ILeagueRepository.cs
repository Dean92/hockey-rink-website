using HockeyRinkAPI.Models;

namespace HockeyRinkAPI.Repositories;

public interface ILeagueRepository
{
    Task<List<League>> GetAllAsync();
    Task<List<League>> GetAllWithTeamsAsync();
    Task<League?> GetByIdAsync(int id);
    Task<League?> GetByIdWithTeamsAsync(int id);
    Task AddAsync(League league);
    Task DeleteAsync(League league);
    Task NullifySessionLeagueIdAsync(int leagueId);
    Task SaveChangesAsync();
}
