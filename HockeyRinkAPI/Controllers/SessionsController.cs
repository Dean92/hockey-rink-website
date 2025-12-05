using System;
using System.ComponentModel.DataAnnotations;
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

            var sessionsQuery = _dbContext.Sessions.Include(s => s.League).Include(s => s.Registrations).AsQueryable();

            if (leagueId.HasValue)
            {
                sessionsQuery = sessionsQuery.Where(s => s.LeagueId == leagueId.Value);
            }

            if (date.HasValue)
            {
                sessionsQuery = sessionsQuery.Where(s => s.StartDate.Date == date.Value.Date);
            }

            var sessions = await sessionsQuery.ToListAsync();

            // Auto-deactivate sessions where dates have passed, but respect manual overrides
            var now = DateTime.UtcNow;
            bool hasChanges = false;
            foreach (var session in sessions)
            {
                // Only auto-deactivate if:
                // 1. Session is currently active
                // 2. Dates have passed
                // 3. Session hasn't been manually modified after the dates passed

                bool shouldAutoDeactivate = false;
                DateTime? criticalDate = null;

                // Check registration close date
                if (session.RegistrationCloseDate.HasValue && session.RegistrationCloseDate.Value < now)
                {
                    criticalDate = session.RegistrationCloseDate.Value;
                    shouldAutoDeactivate = true;
                }
                // Check session end date
                else if (session.EndDate < now)
                {
                    criticalDate = session.EndDate;
                    shouldAutoDeactivate = true;
                }

                // Only deactivate if session is active and either:
                // - Never been manually modified, OR
                // - Last modified before the critical date passed
                if (shouldAutoDeactivate && session.IsActive && criticalDate.HasValue)
                {
                    if (!session.LastModified.HasValue || session.LastModified.Value < criticalDate.Value)
                    {
                        session.IsActive = false;
                        hasChanges = true;
                        _logger.LogInformation(
                            "Auto-deactivated session: {SessionName} (ID: {SessionId}) - Critical date: {CriticalDate}",
                            session.Name,
                            session.Id,
                            criticalDate
                        );
                    }
                }
            }

            if (hasChanges)
            {
                await _dbContext.SaveChangesAsync();
            }

            // Calculate registration counts and spots left for each session
            var sessionDtos = sessions.Select(s => new
            {
                s.Id,
                s.Name,
                s.StartDate,
                s.EndDate,
                s.Fee,
                s.IsActive,
                s.MaxPlayers,
                s.RegistrationOpenDate,
                s.RegistrationCloseDate,
                s.EarlyBirdPrice,
                s.EarlyBirdEndDate,
                s.RegularPrice,
                s.CreatedAt,
                s.LeagueId,
                League = s.League == null ? null : new
                {
                    s.League.Id,
                    s.League.Name,
                    s.League.Description,
                    s.League.StartDate,
                    s.League.EarlyBirdPrice,
                    s.League.EarlyBirdEndDate,
                    s.League.RegularPrice,
                    s.League.RegistrationOpenDate,
                    s.League.RegistrationCloseDate
                },
                RegistrationCount = s.Registrations.Count,
                SpotsLeft = s.MaxPlayers - s.Registrations.Count,
                IsFull = s.Registrations.Count >= s.MaxPlayers
            }).ToList();

            _logger.LogInformation("Found {Count} sessions", sessions.Count);

            return Ok(sessionDtos);
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

            // Check if session is active
            if (!session.IsActive)
            {
                _logger.LogWarning("Session {SessionId} is not active", model.SessionId);
                return BadRequest(new { message = "This session is not accepting registrations" });
            }

            // Check registration window
            var now = DateTime.UtcNow;
            if (session.RegistrationOpenDate.HasValue && session.RegistrationOpenDate.Value > now)
            {
                _logger.LogWarning("Registration not yet open for session {SessionId}", model.SessionId);
                return BadRequest(new { message = "Registration for this session has not opened yet" });
            }

            if (session.RegistrationCloseDate.HasValue && session.RegistrationCloseDate.Value < now)
            {
                _logger.LogWarning("Registration closed for session {SessionId}", model.SessionId);
                return BadRequest(new { message = "Registration for this session has closed" });
            }

            // Check capacity
            var currentRegistrationCount = await _dbContext.SessionRegistrations
                .CountAsync(sr => sr.SessionId == model.SessionId);

            if (currentRegistrationCount >= session.MaxPlayers)
            {
                _logger.LogWarning("Session {SessionId} is full ({CurrentCount}/{MaxPlayers})",
                    model.SessionId, currentRegistrationCount, session.MaxPlayers);
                return BadRequest(new { message = "This session is full" });
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

            // Calculate the amount to charge based on early bird pricing
            var registrationDate = DateTime.UtcNow;
            decimal amountToCharge = session.RegularPrice ?? session.Fee;

            if (session.EarlyBirdPrice.HasValue &&
                session.EarlyBirdEndDate.HasValue &&
                registrationDate <= session.EarlyBirdEndDate.Value)
            {
                amountToCharge = session.EarlyBirdPrice.Value;
                _logger.LogInformation("Applying early bird price: {EarlyBirdPrice}", amountToCharge);
            }

            // Validate age requirement (must be 18+)
            var age = DateTime.UtcNow.Year - model.DateOfBirth.Year;
            if (model.DateOfBirth > DateTime.UtcNow.AddYears(-age)) age--;

            if (age < 18)
            {
                _logger.LogWarning("User age {Age} below minimum requirement", age);
                return BadRequest(new { message = "You must be at least 18 years old to register" });
            }

            var registration = new SessionRegistration
            {
                UserId = userId,
                SessionId = model.SessionId,
                Name = model.Name,
                Address = model.Address,
                City = model.City,
                State = model.State,
                ZipCode = model.ZipCode,
                Phone = model.Phone,
                Email = model.Email,
                DateOfBirth = model.DateOfBirth,
                Position = model.Position,
                RegistrationDate = registrationDate,
                AmountPaid = amountToCharge,
                PaymentStatus = "Pending",
                CreatedAt = DateTime.UtcNow,
            };
            _dbContext.SessionRegistrations.Add(registration);
            await _dbContext.SaveChangesAsync();

            // Process payment
            var transactionId = await _stripeService.ProcessPayment(amountToCharge);
            var payment = new Payment
            {
                SessionRegistrationId = registration.Id,
                Amount = amountToCharge,
                TransactionId = transactionId,
                Status = "Success",
                CreatedAt = DateTime.UtcNow,
            };
            _dbContext.Payments.Add(payment);
            registration.PaymentStatus = "Paid";
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "User {Email} registered for session {SessionId} with transaction {TransactionId}, amount: {Amount}",
                user.Email,
                model.SessionId,
                transactionId,
                amountToCharge
            );
            return Ok(new
            {
                message = "Registered for session successfully",
                amountPaid = amountToCharge,
                transactionId = transactionId
            });
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
    [Required]
    public int SessionId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(50)]
    public string? State { get; set; }

    [StringLength(20)]
    public string? ZipCode { get; set; }

    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [StringLength(20)]
    public string? Position { get; set; } // Forward, Defense, Forward/Defense, Goalie
}
