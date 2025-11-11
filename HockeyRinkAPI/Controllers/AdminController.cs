using System;
using System.Linq;
using System.Threading.Tasks;
using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HockeyRinkAPI.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger
    )
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _logger = logger;
    }

    // Check if user is admin
    private async Task<bool> IsAdminAsync()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length);
            var userId = await GetUserIdFromTokenAsync(token);
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    return await _userManager.IsInRoleAsync(user, "Admin");
                }
            }
        }
        else if (HttpContext.User.Identity?.IsAuthenticated == true)
        {
            var userId = HttpContext
                .User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    return await _userManager.IsInRoleAsync(user, "Admin");
                }
            }
        }

        return false;
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

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var totalUsers = await _dbContext.Users.CountAsync();
            var totalSessions = await _dbContext.Sessions.CountAsync();
            var totalRegistrations = await _dbContext.SessionRegistrations.CountAsync();
            var totalRevenue = await _dbContext
                .Payments.Where(p => p.Status == "Success")
                .SumAsync(p => p.Amount);

            var recentRegistrations = await _dbContext
                .SessionRegistrations.Include(r => r.User)
                .Include(r => r.Session)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .Select(r => new
                {
                    r.Id,
                    UserName = $"{r.User!.FirstName} {r.User.LastName}",
                    UserEmail = r.User.Email,
                    SessionName = r.Session!.Name,
                    r.PaymentStatus,
                    r.CreatedAt,
                })
                .ToListAsync();

            return Ok(
                new
                {
                    totalUsers,
                    totalSessions,
                    totalRegistrations,
                    totalRevenue,
                    recentRegistrations,
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching admin dashboard");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var users = await _dbContext
                .Users.Include(u => u.League)
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.LeagueId,
                    LeagueName = u.League != null ? u.League.Name : null,
                    u.EmailConfirmed,
                    u.CreatedAt,
                })
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpGet("sessions/all")]
    public async Task<IActionResult> GetAllSessions()
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var sessions = await _dbContext
                .Sessions.Include(s => s.League)
                .Include(s => s.Registrations)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.StartDate,
                    s.EndDate,
                    s.Fee,
                    s.IsActive,
                    s.LeagueId,
                    LeagueName = s.League != null ? s.League.Name : null,
                    RegistrationCount = s.Registrations.Count,
                    s.CreatedAt,
                })
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all sessions");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionModel model)
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var session = new Session
            {
                Name = model.Name,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Fee = model.Fee,
                IsActive = model.IsActive,
                LeagueId = model.LeagueId,
                CreatedAt = DateTime.UtcNow,
            };

            _dbContext.Sessions.Add(session);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Session created: {SessionName} (ID: {SessionId})",
                session.Name,
                session.Id
            );
            return Ok(new { message = "Session created successfully", sessionId = session.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPut("sessions/{id}")]
    public async Task<IActionResult> UpdateSession(int id, [FromBody] UpdateSessionModel model)
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var session = await _dbContext.Sessions.FindAsync(id);
            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            session.Name = model.Name;
            session.StartDate = model.StartDate;
            session.EndDate = model.EndDate;
            session.Fee = model.Fee;
            session.IsActive = model.IsActive;
            session.LeagueId = model.LeagueId;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Session updated: {SessionName} (ID: {SessionId})",
                session.Name,
                session.Id
            );
            return Ok(new { message = "Session updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpDelete("sessions/{id}")]
    public async Task<IActionResult> DeleteSession(int id)
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var session = await _dbContext
                .Sessions.Include(s => s.Registrations)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            // Check if there are any registrations
            if (session.Registrations.Any())
            {
                // Instead of deleting, deactivate the session
                session.IsActive = false;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation(
                    "Session deactivated (has registrations): {SessionName} (ID: {SessionId})",
                    session.Name,
                    session.Id
                );
                return Ok(new { message = "Session deactivated (has existing registrations)" });
            }

            _dbContext.Sessions.Remove(session);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Session deleted: {SessionName} (ID: {SessionId})",
                session.Name,
                session.Id
            );
            return Ok(new { message = "Session deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpGet("registrations")]
    public async Task<IActionResult> GetAllRegistrations()
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var registrations = await _dbContext
                .SessionRegistrations.Include(r => r.User)
                .Include(r => r.Session)
                .ThenInclude(s => s!.League)
                .Include(r => r.Payments)
                .Select(r => new
                {
                    r.Id,
                    r.UserId,
                    UserName = $"{r.User!.FirstName} {r.User.LastName}",
                    UserEmail = r.User.Email,
                    r.SessionId,
                    SessionName = r.Session!.Name,
                    LeagueName = r.Session.League != null ? r.Session.League.Name : null,
                    r.PaymentStatus,
                    r.PaymentDate,
                    TotalPaid = r.Payments.Where(p => p.Status == "Success").Sum(p => p.Amount),
                    r.CreatedAt,
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(registrations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching registrations");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }
}

public class CreateSessionModel
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Fee { get; set; }
    public bool IsActive { get; set; } = true;
    public int LeagueId { get; set; }
}

public class UpdateSessionModel
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Fee { get; set; }
    public bool IsActive { get; set; }
    public int LeagueId { get; set; }
}
