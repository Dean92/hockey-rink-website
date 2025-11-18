using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using HockeyRinkAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HockeyRinkAPI.Controllers;

[ApiController]
[Route("api/sessions")]
public class SessionsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly MockStripeService _stripeService;
    private readonly ILogger<SessionsController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public SessionsController(
        AppDbContext dbContext,
        MockStripeService stripeService,
        ILogger<SessionsController> logger,
        UserManager<ApplicationUser> userManager
    )
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _stripeService = stripeService ?? throw new ArgumentNullException(nameof(stripeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    [HttpGet]
    public async Task<IActionResult> GetSessions(
        [FromQuery] int? leagueId,
        [FromQuery] DateTime? date
    )
    {
        try
        {
            _logger.LogInformation(
                "Fetching sessions with leagueId: {LeagueId}, date: {Date}",
                leagueId,
                date
            );

            // Check for token-based auth first
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            _logger.LogInformation("GetSessions - Authorization header: {AuthHeader}", authHeader);

            bool isAuthenticated = false;

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                _logger.LogInformation("GetSessions - Token extracted: {Token}", token);
                isAuthenticated = await ValidateTokenAsync(token);
                _logger.LogInformation(
                    "GetSessions - Token validation result: {IsAuthenticated}",
                    isAuthenticated
                );
            }
            // Fall back to cookie auth
            else if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                _logger.LogInformation("GetSessions - Cookie authenticated");
                isAuthenticated = true;
            }

            if (!isAuthenticated)
            {
                _logger.LogWarning("GetSessions - Not authenticated, returning Unauthorized");
                return Unauthorized(new { message = "Authentication required" });
            }

            var sessionsQuery = _dbContext.Sessions.Include(s => s.League).AsQueryable();

            if (leagueId.HasValue)
            {
                sessionsQuery = sessionsQuery.Where(s => s.LeagueId == leagueId.Value);
            }

            if (date.HasValue)
            {
                sessionsQuery = sessionsQuery.Where(s => s.StartDate.Date == date.Value.Date);
            }

            var sessions = await sessionsQuery.ToListAsync();

            // Auto-deactivate expired sessions
            var today = DateTime.UtcNow.Date;
            bool hasChanges = false;
            foreach (var session in sessions)
            {
                if (session.IsActive && session.EndDate.Date < today)
                {
                    session.IsActive = false;
                    hasChanges = true;
                    _logger.LogInformation(
                        "Auto-deactivated expired session: {SessionName} (ID: {SessionId})",
                        session.Name,
                        session.Id
                    );
                }
            }

            if (hasChanges)
            {
                await _dbContext.SaveChangesAsync();
            }

            _logger.LogInformation("Found {Count} sessions", sessions.Count);

            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch sessions");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] SessionRegistrationModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning(
                    "Invalid model state for session registration: {Errors}",
                    string.Join(
                        ", ",
                        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    )
                );
                return BadRequest(ModelState);
            }

            // Check for token-based auth first
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            _logger.LogInformation(
                "RegisterSession - Authorization header: {AuthHeader}",
                authHeader
            );

            string? userId = null;

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                _logger.LogInformation("RegisterSession - Token extracted");
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
                _logger.LogInformation(
                    "RegisterSession - Cookie authenticated, userId: {UserId}",
                    userId
                );
            }

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("No user ID found for session registration");
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                return Unauthorized(new { message = "User not found" });
            }

            var session = await _dbContext.Sessions.FindAsync(model.SessionId);
            if (session == null)
            {
                _logger.LogWarning("Session not found: {SessionId}", model.SessionId);
                return NotFound(new { message = "Session not found" });
            }

            // Check if user is already registered for this session
            var existingRegistration = await _dbContext.SessionRegistrations.FirstOrDefaultAsync(
                sr => sr.UserId == userId && sr.SessionId == model.SessionId
            );

            if (existingRegistration != null)
            {
                _logger.LogWarning(
                    "User {Email} already registered for session {SessionId}",
                    user.Email,
                    model.SessionId
                );
                return BadRequest(new { message = "You are already registered for this session" });
            }

            var registration = new SessionRegistration
            {
                UserId = userId,
                SessionId = model.SessionId,
                PaymentStatus = "Pending",
                CreatedAt = DateTime.UtcNow,
            };
            _dbContext.SessionRegistrations.Add(registration);
            await _dbContext.SaveChangesAsync();

            // Process payment
            var transactionId = await _stripeService.ProcessPayment(session.Fee);
            var payment = new Payment
            {
                SessionRegistrationId = registration.Id,
                Amount = session.Fee,
                TransactionId = transactionId,
                Status = "Success",
                CreatedAt = DateTime.UtcNow,
            };
            _dbContext.Payments.Add(payment);
            registration.PaymentStatus = "Paid";
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "User {Email} registered for session {SessionId} with transaction {TransactionId}",
                user.Email,
                model.SessionId,
                transactionId
            );
            return Ok(new { message = "Registered for session successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering session for user");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    private async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            _logger.LogInformation("ValidateTokenAsync - Decoding token");
            var tokenData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = tokenData.Split('|');

            if (parts.Length != 3)
            {
                _logger.LogWarning("ValidateTokenAsync - Invalid token format");
                return false;
            }

            var userId = parts[0];
            var email = parts[1];
            var expiry = DateTime.Parse(parts[2]);

            if (expiry < DateTime.UtcNow)
            {
                _logger.LogWarning("ValidateTokenAsync - Token expired");
                return false;
            }

            var user = await _userManager.FindByIdAsync(userId);
            var isValid = user != null && user.Email == email;
            _logger.LogInformation("ValidateTokenAsync - Result: {IsValid}", isValid);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ValidateTokenAsync - Exception occurred");
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

public class SessionRegistrationModel
{
    public int SessionId { get; set; }
}
