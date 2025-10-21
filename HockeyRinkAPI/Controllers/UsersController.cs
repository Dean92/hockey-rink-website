using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HockeyRinkAPI.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext,
        ILogger<UsersController> logger
    )
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            _logger.LogInformation("GetProfile called");

            // Check for token-based auth first
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            _logger.LogInformation("GetProfile - Authorization header: {AuthHeader}", authHeader);

            string? userId = null;

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                var isValid = await ValidateTokenAsync(token);
                if (isValid)
                {
                    userId = await GetUserIdFromTokenAsync(token);
                }
            }
            // Fall back to cookie auth
            else if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("GetProfile - Cookie authenticated, userId: {UserId}", userId);
            }

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("No user ID found in claims for profile request");
                return Unauthorized(new { message = "Invalid or missing authentication" });
            }

            _logger.LogInformation("Fetching profile for user ID: {UserId}", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                return NotFound(new { message = "User not found" });
            }

            return Ok(
                new
                {
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.LeagueId,
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch profile for user");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            _logger.LogInformation("GetDashboard called");

            // Check for token-based auth first
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            _logger.LogInformation("GetDashboard - Authorization header: {AuthHeader}", authHeader);

            string? userId = null;

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                var isValid = await ValidateTokenAsync(token);
                if (isValid)
                {
                    userId = await GetUserIdFromTokenAsync(token);
                }
            }
            // Fall back to cookie auth
            else if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("GetDashboard - Cookie authenticated, userId: {UserId}", userId);
            }

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("No user ID found in claims for dashboard request");
                return Unauthorized(new { message = "Invalid or missing authentication" });
            }

            _logger.LogInformation("Fetching dashboard for user ID: {UserId}", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                return NotFound(new { message = "User not found" });
            }

            var registrations = await _dbContext
                .SessionRegistrations.Where(r => r.UserId == userId)
                .Include(r => r.Session)
                .ThenInclude(s => s!.League)
                .ToListAsync();

            return Ok(
                new
                {
                    User = new
                    {
                        user.FirstName,
                        user.LastName,
                        user.Email,
                        user.LeagueId,
                    },
                    RegisteredSessions = registrations
                        .Select(r => new
                        {
                            r.SessionId,
                            SessionName = r.Session?.Name ?? "Unknown Session",
                            SessionStartDate = r.Session?.StartDate,
                            SessionEndDate = r.Session?.EndDate,
                            SessionFee = r.Session?.Fee,
                            LeagueName = r.Session?.League?.Name,
                        })
                        .ToList(),
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch dashboard for user");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    private async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = tokenData.Split('|');

            if (parts.Length != 3)
                return false;

            var userId = parts[0];
            var email = parts[1];
            var expiry = DateTime.Parse(parts[2]);

            if (expiry < DateTime.UtcNow)
                return false;

            var user = await _userManager.FindByIdAsync(userId);
            return user != null && user.Email == email;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string?> GetUserIdFromTokenAsync(string token)
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