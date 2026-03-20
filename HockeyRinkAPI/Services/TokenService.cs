using HockeyRinkAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace HockeyRinkAPI.Services;

public class TokenService : ITokenService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TokenService> _logger;

    public TokenService(UserManager<ApplicationUser> userManager, ILogger<TokenService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string GenerateToken(ApplicationUser user)
    {
        var expiry = DateTime.UtcNow.AddHours(24);
        var tokenData = $"{user.Id}|{user.Email}|{expiry:O}";
        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenData);
        return Convert.ToBase64String(tokenBytes);
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            _logger.LogInformation("ValidateTokenAsync - Decoding token");
            var tokenData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
            _logger.LogInformation("ValidateTokenAsync - Decoded data: {TokenData}", tokenData);

            var parts = tokenData.Split('|');
            if (parts.Length != 3)
            {
                _logger.LogWarning("ValidateTokenAsync - Invalid token format, parts count: {Count}", parts.Length);
                return false;
            }

            var userId = parts[0];
            var email = parts[1];
            var expiry = DateTime.Parse(parts[2]);

            _logger.LogInformation("ValidateTokenAsync - UserId: {UserId}, Email: {Email}, Expiry: {Expiry}",
                userId, email, expiry);

            if (expiry < DateTime.UtcNow)
            {
                _logger.LogWarning("ValidateTokenAsync - Token expired");
                return false;
            }

            var user = await _userManager.FindByIdAsync(userId);
            var isValid = user != null && user.Email == email;
            _logger.LogInformation("ValidateTokenAsync - User found: {UserFound}, Email match: {EmailMatch}, Result: {IsValid}",
                user != null, user?.Email == email, isValid);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ValidateTokenAsync - Exception occurred");
            return false;
        }
    }

    public async Task<string?> GetUserIdFromTokenAsync(string token)
    {
        try
        {
            var tokenData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = tokenData.Split('|');

            if (parts.Length != 3)
                return null;

            var userId = parts[0];
            var email = parts[1];
            var expiry = DateTime.Parse(parts[2]);

            if (expiry < DateTime.UtcNow)
                return null;

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && user.Email == email)
            {
                return userId;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
