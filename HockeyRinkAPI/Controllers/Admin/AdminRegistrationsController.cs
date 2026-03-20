using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using HockeyRinkAPI.Models.Requests;
using HockeyRinkAPI.Repositories;
using HockeyRinkAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HockeyRinkAPI.Controllers.Admin;

[Route("api/admin")]
public class AdminRegistrationsController : AdminControllerBase
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AdminRegistrationsController> _logger;

    public AdminRegistrationsController(
        ITokenService tokenService,
        UserManager<ApplicationUser> userManager,
        ISessionRepository sessionRepository,
        IRegistrationRepository registrationRepository,
        IPaymentRepository paymentRepository,
        AppDbContext dbContext,
        ILogger<AdminRegistrationsController> logger)
        : base(tokenService, userManager)
    {
        _sessionRepository = sessionRepository;
        _registrationRepository = registrationRepository;
        _paymentRepository = paymentRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("sessions/{id}/registrations")]
    public async Task<IActionResult> GetSessionRegistrations(int id)
    {
        try
        {
            if (!await IsAdminAsync())
                return Forbid();

            var session = await _sessionRepository.GetByIdWithRegistrationsAsync(id);
            if (session == null)
                return NotFound(new { message = "Session not found" });

            var playerAssignments = await _dbContext.Players
                .Include(p => p.Team)
                .Include(p => p.SessionRegistration)
                .Where(p => p.Team.SessionId == id)
                .Select(p => new
                {
                    RegistrationId = p.SessionRegistrationId,
                    TeamId = p.TeamId,
                    TeamName = p.Team.TeamName,
                    JerseyNumber = p.JerseyNumber
                })
                .ToListAsync();

            var registrations = session.SessionRegistrations.Select(r =>
            {
                var assignment = playerAssignments.FirstOrDefault(pa => pa.RegistrationId == r.Id);
                return new
                {
                    r.Id,
                    r.Name,
                    r.Email,
                    r.Phone,
                    r.Position,
                    r.DateOfBirth,
                    r.Address,
                    r.City,
                    r.State,
                    r.ZipCode,
                    r.RegistrationDate,
                    r.AmountPaid,
                    UserId = r.User?.Id,
                    UserEmail = r.User?.Email,
                    AssignedTeam = assignment?.TeamName,
                    TeamId = assignment?.TeamId,
                    JerseyNumber = assignment?.JerseyNumber
                };
            }).OrderByDescending(r => r.RegistrationDate).ToList();

            return Ok(new
            {
                sessionId = session.Id,
                sessionName = session.Name,
                sessionDate = session.StartDate,
                totalRegistrations = registrations.Count,
                maxPlayers = session.MaxPlayers,
                registrations
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching session registrations");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPost("sessions/{id}/registrations/manual")]
    public async Task<IActionResult> AddManualRegistration(int id, [FromBody] ManualRegistrationModel model)
    {
        try
        {
            if (!await IsAdminAsync())
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var session = await _sessionRepository.GetByIdWithRegistrationsAsync(id);
            if (session == null)
                return NotFound(new { message = "Session not found" });

            if (session.SessionRegistrations.Count >= session.MaxPlayers)
                return BadRequest(new { message = "Session is at full capacity" });

            if (session.SessionRegistrations.Any(r => r.Email.ToLower() == model.Email.ToLower()))
                return Conflict(new { message = "User is already registered for this session" });

            ApplicationUser? user = null;
            if (!string.IsNullOrEmpty(model.Email))
            {
                user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        FirstName = model.Name.Split(' ').FirstOrDefault() ?? model.Name,
                        LastName = model.Name.Split(' ').Skip(1).FirstOrDefault() ?? "",
                        Address = model.Address,
                        City = model.City,
                        State = model.State,
                        ZipCode = model.ZipCode,
                        Phone = model.Phone,
                        DateOfBirth = model.DateOfBirth,
                        Position = model.Position,
                        IsManuallyRegistered = true,
                        PasswordSetupToken = Guid.NewGuid().ToString(),
                        PasswordSetupTokenExpiry = DateTime.UtcNow.AddDays(7),
                        EmailConfirmed = false
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                        return BadRequest(new { message = "Failed to create user account", errors = result.Errors });

                    _logger.LogInformation(
                        "Created manual registration user account for {Email} with setup token {Token}",
                        user.Email, user.PasswordSetupToken);
                }
            }

            var registration = new SessionRegistration
            {
                SessionId = id,
                UserId = user?.Id,
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                City = model.City,
                State = model.State,
                ZipCode = model.ZipCode,
                DateOfBirth = DateOnly.FromDateTime(model.DateOfBirth),
                Position = model.Position,
                RegistrationDate = DateTime.UtcNow,
                AmountPaid = model.AmountPaid
            };

            await _registrationRepository.AddAsync(registration);
            await _registrationRepository.SaveChangesAsync();

            var payment = new Payment
            {
                SessionRegistrationId = registration.Id,
                Amount = model.AmountPaid,
                TransactionId = $"MANUAL-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Status = "Completed"
            };

            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Admin manually registered {Name} ({Email}) for session {SessionId}",
                model.Name, model.Email, id);

            return Ok(new
            {
                message = $"{model.Name} successfully registered for session",
                registrationId = registration.Id,
                passwordSetupToken = user?.PasswordSetupToken,
                passwordSetupRequired = user?.IsManuallyRegistered ?? false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding manual registration");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPut("sessions/{sessionId}/registrations/{registrationId}")]
    public async Task<IActionResult> UpdateRegistration(int sessionId, int registrationId, [FromBody] ManualRegistrationModel model)
    {
        try
        {
            if (!await IsAdminAsync())
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var registration = await _registrationRepository.GetByIdAndSessionAsync(registrationId, sessionId);
            if (registration == null)
                return NotFound(new { message = "Registration not found" });

            registration.Name = model.Name;
            registration.Email = model.Email;
            registration.Phone = model.Phone;
            registration.Address = model.Address;
            registration.City = model.City;
            registration.State = model.State;
            registration.ZipCode = model.ZipCode;
            registration.DateOfBirth = DateOnly.FromDateTime(model.DateOfBirth);
            registration.Position = model.Position;
            registration.AmountPaid = model.AmountPaid;

            var payment = await _paymentRepository.GetByRegistrationIdAsync(registrationId);
            if (payment != null)
                payment.Amount = model.AmountPaid;

            if (model.JerseyNumber.HasValue)
            {
                var player = await _dbContext.Players
                    .FirstOrDefaultAsync(p => p.SessionRegistrationId == registrationId);

                if (player != null)
                {
                    var conflictingPlayer = await _dbContext.Players
                        .FirstOrDefaultAsync(p => p.TeamId == player.TeamId &&
                                                  p.JerseyNumber == model.JerseyNumber &&
                                                  p.Id != player.Id);

                    if (conflictingPlayer != null)
                        return BadRequest(new { message = $"Jersey number {model.JerseyNumber} is already assigned to another player on this team" });

                    player.JerseyNumber = model.JerseyNumber;
                    player.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _registrationRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Admin updated registration {RegistrationId} for session {SessionId}",
                registrationId, sessionId);

            return Ok(new { message = $"Registration for {model.Name} updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating registration");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpDelete("sessions/{sessionId}/registrations/{registrationId}")]
    public async Task<IActionResult> RemoveRegistration(int sessionId, int registrationId)
    {
        try
        {
            if (!await IsAdminAsync())
                return Forbid();

            var registration = await _registrationRepository.GetByIdAndSessionAsync(registrationId, sessionId);
            if (registration == null)
                return NotFound(new { message = "Registration not found" });

            var payment = await _paymentRepository.GetByRegistrationIdAsync(registrationId);
            if (payment != null)
                _paymentRepository.Remove(payment);

            _registrationRepository.Remove(registration);
            await _registrationRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Admin removed registration {RegistrationId} ({Name}) from session {SessionId}",
                registrationId, registration.Name, sessionId);

            return Ok(new { message = $"{registration.Name} removed from session. Note: Refunds must be processed manually." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing registration");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpGet("registrations")]
    public async Task<IActionResult> GetAllRegistrations()
    {
        try
        {
            if (!await IsAdminAsync())
                return Forbid();

            var registrationEntities = await _registrationRepository.GetAllWithDetailsAsync();
            var registrations = registrationEntities
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
                .ToList();

            return Ok(registrations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching registrations");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }
}
