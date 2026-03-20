using HockeyRinkAPI.Models;

namespace HockeyRinkAPI.Repositories;

public interface ISessionRepository
{
    Task<List<Session>> GetAllWithDetailsAsync();
    Task<List<Session>> GetFilteredAsync(int? leagueId, DateTime? date);
    Task<Session?> GetByIdAsync(int id);
    Task<Session?> GetByIdWithRegistrationsAsync(int id);
    Task<Session?> GetByIdWithLeagueAsync(int id);
    Task<List<Session>> GetActiveSessionsWithDetailsAsync(DateTime now);
    Task<List<Session>> GetUpcomingAsync(DateTime from, DateTime to);
    Task AddAsync(Session session);
    void Remove(Session session);
    Task SaveChangesAsync();
}
