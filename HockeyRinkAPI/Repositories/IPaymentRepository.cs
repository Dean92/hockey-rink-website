using HockeyRinkAPI.Models;

namespace HockeyRinkAPI.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByRegistrationIdAsync(int registrationId);
    Task AddAsync(Payment payment);
    void Remove(Payment payment);
    Task SaveChangesAsync();
}
