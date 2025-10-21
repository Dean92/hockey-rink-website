using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HockeyRinkAPI.Controllers;

[ApiController]
[Route("api/leagues")]
public class LeaguesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LeaguesController> _logger;

    public LeaguesController(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger<LeaguesController> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetLeagues()
    {
        try
        {
            _logger.LogInformation("GetLeagues - Request received");

            // Check for token-based auth first
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            _logger.LogInformation("GetLeagues - Authorization header: {AuthHeader}", authHeader);

            bool isAuthenticated = false;

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                _logger.LogInformation("GetLeagues - Token extracted: {Token}", token);
                isAuthenticated = await ValidateTokenAsync(token);
                _logger.LogInformation("GetLeagues - Token validation result: {IsAuthenticated}", isAuthenticated);
            }
            // Fall back to cookie auth
            else if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                _logger.LogInformation("GetLeagues - Cookie authenticated");
                isAuthenticated = true;
            }

            if (!isAuthenticated)
            {
                _logger.LogWarning("GetLeagues - Not authenticated, returning Unauthorized");
                return Unauthorized(new { message = "Authentication required" });
            }

            _logger.LogInformation("GetLeagues - Authentication successful, fetching leagues");
            var leagues = await _db.Leagues.ToListAsync();
            _logger.LogInformation("GetLeagues - Found {Count} leagues", leagues.Count);

            return Ok(leagues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetLeagues - Error occurred");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    private async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            _logger.LogInformation("ValidateTokenAsync - Token: {Token}", token);
            var tokenData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
            _logger.LogInformation("ValidateTokenAsync - Decoded token data: {TokenData}", tokenData);
            var parts = tokenData.Split('|');

            if (parts.Length != 3)
            {
                _logger.LogWarning("ValidateTokenAsync - Invalid parts count: {Count}", parts.Length);
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
            _logger.LogError(ex, "ValidateTokenAsync - Exception: {Message}", ex.Message);
            return false;
        }
    }
}