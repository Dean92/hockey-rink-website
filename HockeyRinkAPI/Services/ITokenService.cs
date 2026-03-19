using HockeyRinkAPI.Models;

namespace HockeyRinkAPI.Services;

public interface ITokenService
{
    string GenerateToken(ApplicationUser user);
    Task<bool> ValidateTokenAsync(string token);
    Task<string?> GetUserIdFromTokenAsync(string token);
}
