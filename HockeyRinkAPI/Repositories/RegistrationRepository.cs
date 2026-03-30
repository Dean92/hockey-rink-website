using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HockeyRinkAPI.Repositories;

public class RegistrationRepository : IRegistrationRepository
{
    private readonly AppDbContext _dbContext;

    public RegistrationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<SessionRegistration>> GetByUserIdAsync(string userId) =>
        _dbContext.SessionRegistrations
            .Include(sr => sr.Session)
                .ThenInclude(s => s!.League)
            .Where(sr => sr.UserId == userId)
            .OrderByDescending(sr => sr.Session!.StartDate)
            .ToListAsync();

    public Task<List<SessionRegistration>> GetAllWithDetailsAsync() =>
        _dbContext.SessionRegistrations
            .Include(r => r.User)
            .Include(r => r.Session)
                .ThenInclude(s => s!.League)
            .Include(r => r.Payments)
            .ToListAsync();

    public Task<List<SessionRegistration>> GetByDateRangeAsync(DateTime from, DateTime to) =>
        _dbContext.SessionRegistrations
            .Where(r => r.RegistrationDate >= from && r.RegistrationDate < to)
            .ToListAsync();

    public Task<SessionRegistration?> GetByIdAndSessionAsync(int id, int sessionId) =>
        _dbContext.SessionRegistrations
            .Include(r => r.Session)
            .FirstOrDefaultAsync(r => r.Id == id && r.SessionId == sessionId);

    public Task<SessionRegistration?> GetByIdForUserAsync(int id, string userId) =>
        _dbContext.SessionRegistrations
            .Include(sr => sr.Session)
            .FirstOrDefaultAsync(sr => sr.Id == id && sr.UserId == userId);

    public Task<SessionRegistration?> GetCurrentForUserAsync(string userId, DateTime now) =>
        _dbContext.SessionRegistrations
            .Include(sr => sr.Session)
                .ThenInclude(s => s.League)
            .Where(sr => sr.UserId == userId
                && sr.Session.StartDate <= now
                && sr.Session.EndDate >= now)
            .OrderBy(sr => sr.Session.StartDate)
            .FirstOrDefaultAsync();

    public Task<SessionRegistration?> GetByUserAndSessionAsync(string userId, int sessionId) =>
        _dbContext.SessionRegistrations
            .FirstOrDefaultAsync(sr => sr.UserId == userId && sr.SessionId == sessionId);

    public Task<bool> ExistsAsync(string userId, int sessionId) =>
        _dbContext.SessionRegistrations
            .AnyAsync(sr => sr.UserId == userId && sr.SessionId == sessionId);

    public Task<int> CountBySessionAsync(int sessionId) =>
        _dbContext.SessionRegistrations
            .CountAsync(sr => sr.SessionId == sessionId);

    public Task<decimal> GetTotalRevenueAsync() =>
        _dbContext.SessionRegistrations.SumAsync(r => r.AmountPaid);

    public Task<decimal> GetRevenueFromDateAsync(DateTime from) =>
        _dbContext.SessionRegistrations
            .Where(r => r.RegistrationDate >= from)
            .SumAsync(r => r.AmountPaid);

    public Task<decimal> GetYearRevenueAsync(int year) =>
        _dbContext.SessionRegistrations
            .Where(r => r.RegistrationDate.Year == year)
            .SumAsync(r => r.AmountPaid);

    public async Task AddAsync(SessionRegistration registration) =>
        await _dbContext.SessionRegistrations.AddAsync(registration);

    public void Remove(SessionRegistration registration) =>
        _dbContext.SessionRegistrations.Remove(registration);

    public Task SaveChangesAsync() =>
        _dbContext.SaveChangesAsync();
}
