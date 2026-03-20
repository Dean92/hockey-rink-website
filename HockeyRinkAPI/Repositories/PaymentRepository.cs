using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HockeyRinkAPI.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _dbContext;

    public PaymentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Payment?> GetByRegistrationIdAsync(int registrationId) =>
        _dbContext.Payments
            .FirstOrDefaultAsync(p => p.SessionRegistrationId == registrationId);

    public async Task AddAsync(Payment payment) =>
        await _dbContext.Payments.AddAsync(payment);

    public void Remove(Payment payment) =>
        _dbContext.Payments.Remove(payment);

    public Task SaveChangesAsync() =>
        _dbContext.SaveChangesAsync();
}
