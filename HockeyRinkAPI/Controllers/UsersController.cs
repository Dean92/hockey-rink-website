using System;
using System.ComponentModel.DataAnnotations;
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

            // Determine current league based on active sessions (sessions that have started but not ended)
            var now = DateTime.UtcNow;
            var currentSession = await _dbContext.SessionRegistrations
                .Include(sr => sr.Session)
                    .ThenInclude(s => s.League)
                .Where(sr => sr.UserId == userId
                    && sr.Session.StartDate <= now
                    && sr.Session.EndDate >= now)
                .OrderBy(sr => sr.Session.StartDate)
                .FirstOrDefaultAsync();

            int? currentLeagueId = currentSession?.Session.LeagueId;
            string? currentLeagueName = currentSession?.Session.League?.Name;

            return Ok(
                new
                {
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    LeagueId = currentLeagueId,
                    LeagueName = currentLeagueName,
                    user.Address,
                    user.City,
                    user.State,
                    user.ZipCode,
                    user.Phone,
                    user.DateOfBirth,
                    user.Position
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch profile for user");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get authenticated user ID
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
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
            else if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Invalid or missing authentication" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Update profile fields
            user.Address = model.Address;
            user.City = model.City;
            user.State = model.State;
            user.ZipCode = model.ZipCode;
            user.Phone = model.Phone;
            user.DateOfBirth = model.DateOfBirth;
            user.Position = model.Position;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Failed to update profile", errors = result.Errors });
            }

            _logger.LogInformation("Profile updated for user {Email}", user.Email);

            return Ok(new
            {
                message = "Profile updated successfully",
                user.Email,
                user.FirstName,
                user.LastName,
                user.Address,
                user.City,
                user.State,
                user.ZipCode,
                user.Phone,
                user.DateOfBirth,
                user.Position
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update profile");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get authenticated user ID
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
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
            else if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Invalid or missing authentication" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Verify current password
            var passwordCheck = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
            if (!passwordCheck)
            {
                return BadRequest(new { message = "Current password is incorrect" });
            }

            // Change password
            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Password changed for user {Email}", user.Email);

            return Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change password");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpGet("my-sessions")]
    public async Task<IActionResult> GetMySessions()
    {
        try
        {
            // Get authenticated user ID
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
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
            else if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Invalid or missing authentication" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Get all session registrations for this user
            var registrations = await _dbContext.SessionRegistrations
                .Include(sr => sr.Session)
                .ThenInclude(s => s!.League)
                .Where(sr => sr.UserId == userId)
                .OrderByDescending(sr => sr.Session!.StartDate)
                .ToListAsync();

            var now = DateTime.UtcNow;

            var upcomingSessions = registrations
                .Where(sr => sr.Session != null && sr.Session.StartDate > now)
                .Select(sr => new
                {
                    registrationId = sr.Id,
                    sessionId = sr.Session!.Id,
                    sessionName = sr.Session.Name,
                    startDate = sr.Session.StartDate,
                    endDate = sr.Session.EndDate,
                    leagueName = sr.Session.League?.Name,
                    amountPaid = sr.AmountPaid,
                    registrationDate = sr.RegistrationDate,
                    paymentStatus = sr.PaymentStatus,
                    position = sr.Position
                })
                .ToList();

            var currentSessions = registrations
                .Where(sr => sr.Session != null && sr.Session.StartDate <= now && sr.Session.EndDate >= now)
                .Select(sr => new
                {
                    registrationId = sr.Id,
                    sessionId = sr.Session!.Id,
                    sessionName = sr.Session.Name,
                    startDate = sr.Session.StartDate,
                    endDate = sr.Session.EndDate,
                    leagueName = sr.Session.League?.Name,
                    amountPaid = sr.AmountPaid,
                    registrationDate = sr.RegistrationDate,
                    paymentStatus = sr.PaymentStatus,
                    position = sr.Position
                })
                .ToList();

            var pastSessions = registrations
                .Where(sr => sr.Session != null && sr.Session.EndDate < now)
                .Select(sr => new
                {
                    registrationId = sr.Id,
                    sessionId = sr.Session!.Id,
                    sessionName = sr.Session.Name,
                    startDate = sr.Session.StartDate,
                    endDate = sr.Session.EndDate,
                    leagueName = sr.Session.League?.Name,
                    amountPaid = sr.AmountPaid,
                    registrationDate = sr.RegistrationDate,
                    paymentStatus = sr.PaymentStatus,
                    position = sr.Position
                })
                .ToList();

            _logger.LogInformation("Returning {UpcomingCount} upcoming, {CurrentCount} current, and {PastCount} past sessions for user {Email}",
                upcomingSessions.Count, currentSessions.Count, pastSessions.Count, user.Email);

            return Ok(new
            {
                upcomingSessions,
                currentSessions,
                pastSessions,
                totalRegistrations = registrations.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch user sessions");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpDelete("my-sessions/{registrationId}")]
    public async Task<IActionResult> CancelSessionRegistration(int registrationId)
    {
        try
        {
            // Get authenticated user ID
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
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
            else if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Invalid or missing authentication" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Find the registration
            var registration = await _dbContext.SessionRegistrations
                .Include(sr => sr.Session)
                .FirstOrDefaultAsync(sr => sr.Id == registrationId && sr.UserId == userId);

            if (registration == null)
            {
                return NotFound(new { message = "Registration not found" });
            }

            // Check if session has already started
            if (registration.Session != null && registration.Session.StartDate <= DateTime.UtcNow)
            {
                return BadRequest(new { message = "Cannot cancel registration for a session that has already started" });
            }

            // Check if cancellation is within 7 days of session start
            if (registration.Session != null)
            {
                var daysUntilStart = (registration.Session.StartDate - DateTime.UtcNow).TotalDays;
                if (daysUntilStart < 7)
                {
                    return BadRequest(new { message = "Cancellations must be made at least 7 days before the session start date" });
                }
            }

            // Find and remove associated payment record
            var payment = await _dbContext.Payments
                .FirstOrDefaultAsync(p => p.SessionRegistrationId == registrationId);

            if (payment != null)
            {
                _dbContext.Payments.Remove(payment);
            }

            // Remove the registration
            _dbContext.SessionRegistrations.Remove(registration);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User {Email} cancelled registration {RegistrationId} for session {SessionName}",
                user.Email, registrationId, registration.Session?.Name);

            return Ok(new
            {
                message = "Cancellation request submitted successfully. Refunds will be processed within 7-14 business days.",
                refundAmount = registration.AmountPaid,
                sessionName = registration.Session?.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel registration");
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

    /// <summary>
    /// Get user's team assignment for a specific session (published drafts only)
    /// </summary>
    [HttpGet("my-team/{sessionId}")]
    [Authorize]
    public async Task<IActionResult> GetMyTeam(int sessionId)
    {
        try
        {
            var userId = await GetUserIdFromRequestAsync();
            if (userId == null)
            {
                return Unauthorized(new { message = "Invalid or missing authentication" });
            }

            var session = await _dbContext.Sessions
                .Include(s => s.League)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            if (!session.DraftPublished)
            {
                return NotFound(new { message = "Draft not yet published for this session" });
            }

            var registration = await _dbContext.SessionRegistrations
                .FirstOrDefaultAsync(sr => sr.SessionId == sessionId && sr.UserId == userId);

            if (registration == null)
            {
                return NotFound(new { message = "You are not registered for this session" });
            }

            var player = await _dbContext.Players
                .Include(p => p.Team)
                    .ThenInclude(t => t.Captain)
                .Include(p => p.SessionRegistration)
                .FirstOrDefaultAsync(p => p.SessionRegistrationId == registration.Id);

            if (player == null || player.TeamId == null)
            {
                return NotFound(new { message = "You have not been assigned to a team yet" });
            }

            var team = player.Team;
            var isCaptain = team.CaptainUserId == userId;

            var teammates = await _dbContext.Players
                .Include(p => p.SessionRegistration)
                    .ThenInclude(sr => sr.User)
                .Where(p => p.TeamId == team.Id)
                .OrderByDescending(p => p.SessionRegistration.UserId == team.CaptainUserId)
                .ThenBy(p => p.SessionRegistration.Name)
                .Select(p => new
                {
                    Name = p.SessionRegistration.Name,
                    Position = p.SessionRegistration.Position,
                    Email = isCaptain ? p.SessionRegistration.User!.Email : null
                })
                .ToListAsync();

            return Ok(new
            {
                TeamId = team.Id,
                TeamName = team.TeamName,
                TeamColor = team.TeamColor,
                CaptainName = team.CaptainName,
                IsCaptain = isCaptain,
                Teammates = teammates,
                SessionName = session.Name,
                SessionDate = session.StartDate,
                LeagueName = session.League?.Name,
                SessionRecord = (string?)null,
                Standing = (string?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team for user in session {SessionId}", sessionId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all team assignments for current user
    /// </summary>
    [HttpGet("my-teams")]
    [Authorize]
    public async Task<IActionResult> GetMyTeams()
    {
        try
        {
            var userId = await GetUserIdFromRequestAsync();
            if (userId == null)
            {
                return Unauthorized(new { message = "Invalid or missing authentication" });
            }

            var teams = await _dbContext.SessionRegistrations
                .Include(sr => sr.Session)
                    .ThenInclude(s => s.League)
                .Where(sr => sr.UserId == userId)
                .Select(sr => new
                {
                    sr.Session,
                    sr.Session.League,
                    Player = _dbContext.Players
                        .Include(p => p.Team)
                        .FirstOrDefault(p => p.SessionRegistrationId == sr.Id)
                })
                .Where(x => x.Session.DraftPublished && x.Player != null && x.Player.TeamId != null)
                .Select(x => new
                {
                    SessionId = x.Session.Id,
                    SessionName = x.Session.Name,
                    SessionDate = x.Session.StartDate,
                    LeagueName = x.League.Name,
                    TeamId = x.Player!.TeamId,
                    TeamName = x.Player.Team!.TeamName,
                    TeamColor = x.Player.Team.TeamColor,
                    IsCaptain = x.Player.Team.CaptainUserId == userId,
                    SessionRecord = (string?)null,
                    Standing = (string?)null
                })
                .OrderByDescending(x => x.SessionDate)
                .ToListAsync();

            return Ok(teams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all teams for user");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    private async Task<string?> GetUserIdFromRequestAsync()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length);
            var isValid = await ValidateTokenAsync(token);
            if (isValid)
            {
                return await GetUserIdFromTokenAsync(token);
            }
        }
        else if (HttpContext.User.Identity?.IsAuthenticated == true)
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        return null;
    }
}
