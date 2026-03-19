using HockeyRinkAPI.Models;

namespace HockeyRinkAPI.Services;

public interface ISessionActivationService
{
    Task<bool> ApplyActivationRulesAsync(IEnumerable<Session> sessions);
}
