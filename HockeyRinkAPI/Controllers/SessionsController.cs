using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HockeyRinkAPI.Models;
using HockeyRinkAPI.Repositories;
using HockeyRinkAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HockeyRinkAPI.Controllers;

[ApiController]
[Route("api/sessions")]
public class SessionsController : ControllerBase
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<SessionsController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ISessionActivationService _sessionActivationService;

    public SessionsController(
        ISessionRepository sessionRepository,
        IRegistrationRepository registrationRepository,
        IPaymentRepository paymentRepository,
        IPaymentService paymentService,
        ILogger<SessionsController> logger,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ISessionActivationService sessionActivationService
    )
    {
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _registrationRepository = registrationRepository ?? throw new ArgumentNullException(nameof(registrationRepository));
        _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _sessionActivationService = sessionActivationService ?? throw new ArgumentNullException(nameof(sessionActivationService));
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

            var sessions = await _sessionRepository.GetFilteredAsync(leagueId, date);

            var now = DateTime.UtcNow;
            var hasChanges = await _sessionActivationService.ApplyActivationRulesAsync(sessions);
            if (hasChanges)
            {
                await _sessionRepository.SaveChangesAsync();
            }

            // Calculate registration counts and spots left for each session
            var sessionDtos = sessions.Select(s => new
            {
                s.Id,
                s.Name,
                s.Description,
                s.StartDate,
                s.StartTime,
                s.EndDate,
                s.EndTime,
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
                RegistrationCount = s.SessionRegistrations.Count,
                SpotsLeft = s.MaxPlayers - s.SessionRegistrations.Count,
                IsFull = s.SessionRegistrations.Count >= s.MaxPlayers,
                IsRegistrationOpen = (!s.RegistrationOpenDate.HasValue || s.RegistrationOpenDate.Value <= now) &&
                                    (!s.RegistrationCloseDate.HasValue || s.RegistrationCloseDate.Value > now)
            })
            .OrderByDescending(s => s.IsRegistrationOpen)
            .ThenBy(s => s.RegistrationCloseDate ?? DateTime.MaxValue)
            .ThenBy(s => s.StartDate)
            .ToList();

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
                var isValid = await _tokenService.ValidateTokenAsync(token);
                if (isValid)
                {
                    userId = await _tokenService.GetUserIdFromTokenAsync(token);
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

            var session = await _sessionRepository.GetByIdAsync(model.SessionId);
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
            var currentRegistrationCount = await _registrationRepository.CountBySessionAsync(model.SessionId);

            if (currentRegistrationCount >= session.MaxPlayers)
            {
                _logger.LogWarning("Session {SessionId} is full ({CurrentCount}/{MaxPlayers})",
                    model.SessionId, currentRegistrationCount, session.MaxPlayers);
                return BadRequest(new { message = "This session is full" });
            }

            // Check if user is already registered for this session
            var existingRegistration = await _registrationRepository.GetByUserAndSessionAsync(userId, model.SessionId);

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

            // Update user profile with registration information
            user.Address = model.Address;
            user.City = model.City;
            user.State = model.State;
            user.ZipCode = model.ZipCode;
            user.Phone = model.Phone;
            user.DateOfBirth = model.DateOfBirth;
            user.Position = model.Position;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogWarning("Failed to update user profile during registration");
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
                DateOfBirth = DateOnly.FromDateTime(model.DateOfBirth),
                Position = model.Position,
                RegistrationDate = registrationDate,
                AmountPaid = amountToCharge,
                PaymentStatus = "Pending",
                CreatedAt = DateTime.UtcNow,
            };
            await _registrationRepository.AddAsync(registration);
            await _registrationRepository.SaveChangesAsync();

            // Process payment using MockPaymentService
            _logger.LogInformation("Processing payment for session {SessionId}, amount: {Amount}",
                model.SessionId, amountToCharge);

            var paymentRequest = new PaymentRequest
            {
                CardNumber = model.CardNumber,
                ExpiryDate = model.ExpiryDate,
                Cvv = model.Cvv,
                CardholderName = model.CardholderName,
                Amount = amountToCharge,
                Description = $"Registration for {session.Name}"
            };

            var paymentResponse = await _paymentService.ProcessPaymentAsync(paymentRequest);

            if (!paymentResponse.Success)
            {
                _logger.LogWarning("Payment failed for session {SessionId}: {Error}",
                    model.SessionId, paymentResponse.ErrorMessage);

                // Remove the registration since payment failed
                _registrationRepository.Remove(registration);
                await _registrationRepository.SaveChangesAsync();

                return BadRequest(new
                {
                    message = "Payment failed",
                    error = paymentResponse.ErrorMessage
                });
            }

            // Payment successful - create payment record
            var payment = new Payment
            {
                SessionRegistrationId = registration.Id,
                Amount = amountToCharge,
                TransactionId = paymentResponse.TransactionId!,
                Status = "Success",
                CreatedAt = paymentResponse.ProcessedAt,
            };
            await _paymentRepository.AddAsync(payment);
            registration.PaymentStatus = "Paid";
            registration.PaymentDate = paymentResponse.ProcessedAt;
            await _paymentRepository.SaveChangesAsync();

            _logger.LogInformation(
                "User {Email} registered for session {SessionId} with transaction {TransactionId}, amount: ${Amount}",
                user.Email,
                model.SessionId,
                paymentResponse.TransactionId,
                amountToCharge
            );

            return Ok(new
            {
                message = $"Successfully registered for {session.Name}",
                amountPaid = amountToCharge,
                transactionId = paymentResponse.TransactionId,
                sessionName = session.Name,
                startDate = session.StartDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering session for user");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
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

    // Payment fields
    [Required]
    [StringLength(16, MinimumLength = 16)]
    public string CardNumber { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Expiry date must be in MM/YY format")]
    public string ExpiryDate { get; set; } = string.Empty;

    [Required]
    [StringLength(4, MinimumLength = 3)]
    public string Cvv { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string CardholderName { get; set; } = string.Empty;
}
