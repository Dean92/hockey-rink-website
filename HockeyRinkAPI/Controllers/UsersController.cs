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

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("No user ID found in claims for profile request");
                return Unauthorized(new { Message = "Invalid or missing authentication" });
            }

            _logger.LogInformation("Fetching profile for user ID: {UserId}", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                return NotFound(new { Message = "User not found" });
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
            return StatusCode(500, new { Error = "Internal Server Error", Details = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("No user ID found in claims for dashboard request");
                return Unauthorized(new { Message = "Invalid or missing authentication" });
            }

            _logger.LogInformation("Fetching dashboard for user ID: {UserId}", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                return NotFound(new { Message = "User not found" });
            }

            var registrations = await _dbContext
                .SessionRegistrations.Where(r => r.UserId == userId)
                .Include(r => r.Session)
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
                            SessionName = r.Session.Name ?? "Unknown Session",
                            SessionStartDate = r.Session.StartDate,
                            SessionEndDate = r.Session.EndDate,
                            SessionFee = r.Session.Fee,
                        })
                        .ToList(),
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch dashboard for user");
            return StatusCode(500, new { Error = "Internal Server Error", Details = ex.Message });
        }
    }
}
