using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using HockeyRinkAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
[ApiController]
[Route("api/sessions")]
public class SessionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly MockStripeService _stripe;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(AppDbContext db, MockStripeService stripe, ILogger<SessionsController> logger)
    {
        _db = db;
        _stripe = stripe;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetSessions()
    {
        try
        {
            var sessions = await _db.Sessions.ToListAsync();
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetSessions");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] SessionRegistrationModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for session registration: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            if (User?.Identity?.Name == null)
            {
                _logger.LogWarning("User identity is null");
                return Unauthorized(new { Message = "User not authenticated" });
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
            if (user == null || user.LeagueId == null)
            {
                _logger.LogWarning("User not found or not assigned to a league: {Email}", User.Identity.Name);
                return BadRequest(new { Message = "User not assigned to a league" });
            }

            var session = await _db.Sessions.FindAsync(model.SessionId);
            if (session == null)
            {
                _logger.LogWarning("Session not found: {SessionId}", model.SessionId);
                return NotFound(new { Message = "Session not found" });
            }

            var registration = new SessionRegistration
            {
                UserId = user.Id,
                SessionId = model.SessionId,
                PaymentStatus = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            _db.SessionRegistrations.Add(registration);
            await _db.SaveChangesAsync();

            var transactionId = await _stripe.ProcessPayment(session.Fee);
            var payment = new Payment
            {
                SessionRegistrationId = registration.Id,
                Amount = session.Fee,
                TransactionId = transactionId,
                Status = "Success",
                CreatedAt = DateTime.UtcNow
            };
            _db.Payments.Add(payment);
            registration.PaymentStatus = "Paid";
            await _db.SaveChangesAsync();

            _logger.LogInformation("User {Email} registered for session {SessionId}", user.Email, model.SessionId);
            return Ok(new { Message = "Registered for session" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Register");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }
}

public class SessionRegistrationModel
{
    public int SessionId { get; set; }
}