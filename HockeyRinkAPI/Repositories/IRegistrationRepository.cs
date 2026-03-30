using HockeyRinkAPI.Models;

namespace HockeyRinkAPI.Repositories;

public interface IRegistrationRepository
{
    Task<List<SessionRegistration>> GetByUserIdAsync(string userId);
    Task<List<SessionRegistration>> GetAllWithDetailsAsync();
    Task<List<SessionRegistration>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<SessionRegistration?> GetByIdAndSessionAsync(int id, int sessionId);
    Task<SessionRegistration?> GetByIdForUserAsync(int id, string userId);
    Task<SessionRegistration?> GetCurrentForUserAsync(string userId, DateTime now);
    Task<SessionRegistration?> GetByUserAndSessionAsync(string userId, int sessionId);
    Task<bool> ExistsAsync(string userId, int sessionId);
    Task<int> CountBySessionAsync(int sessionId);
    Task<decimal> GetTotalRevenueAsync();
    Task<decimal> GetRevenueFromDateAsync(DateTime from);
    Task<decimal> GetYearRevenueAsync(int year);
    Task AddAsync(SessionRegistration registration);
    void Remove(SessionRegistration registration);
    Task SaveChangesAsync();
}
