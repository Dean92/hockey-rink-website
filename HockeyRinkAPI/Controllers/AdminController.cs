using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using HockeyRinkAPI.Models.Requests;
using HockeyRinkAPI.Repositories;
using HockeyRinkAPI.Services;
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
    private readonly ITokenService _tokenService;
    private readonly ISessionActivationService _sessionActivationService;
    private readonly ISessionRepository _sessionRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly ILeagueRepository _leagueRepository;
    private readonly IPaymentRepository _paymentRepository;

    public AdminController(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger,
        ITokenService tokenService,
        ISessionActivationService sessionActivationService,
        ISessionRepository sessionRepository,
        IRegistrationRepository registrationRepository,
        ILeagueRepository leagueRepository,
        IPaymentRepository paymentRepository
    )
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _logger = logger;
        _tokenService = tokenService;
        _sessionActivationService = sessionActivationService;
        _sessionRepository = sessionRepository;
        _registrationRepository = registrationRepository;
        _leagueRepository = leagueRepository;
        _paymentRepository = paymentRepository;
    }

    // Check if user is admin
    private async Task<bool> IsAdminAsync()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length);
            var userId = await _tokenService.GetUserIdFromTokenAsync(token);
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
                    // Get league name from user's LeagueId or their most recent session registration
                    LeagueName = u.League != null ? u.League.Name :
                        _dbContext.SessionRegistrations
                            .Where(sr => sr.UserId == u.Id)
                            .OrderByDescending(sr => sr.RegistrationDate)
                            .Select(sr => sr.Session.League.Name)
                            .FirstOrDefault(),
                    u.EmailConfirmed,
                    u.CreatedAt,
                    u.Rating,
                    u.PlayerNotes,
                    u.Position,
                    u.Address,
                    u.City,
                    u.State,
                    u.ZipCode,
                    u.Phone,
                    u.DateOfBirth,
                    u.LastLoginAt,
                    u.EmergencyContactName,
                    u.EmergencyContactPhone,
                    u.HockeyRegistrationNumber,
                    u.HockeyRegistrationType
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

            var sessions = await _sessionRepository.GetAllWithDetailsAsync();

            var hasChanges = await _sessionActivationService.ApplyActivationRulesAsync(sessions);
            if (hasChanges)
            {
                await _sessionRepository.SaveChangesAsync();
            }

            var result = sessions
                .Select(s => new
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
                    s.DraftEnabled,
                    s.DraftPublished,
                    s.LeagueId,
                    LeagueName = s.League != null ? s.League.Name : null,
                    RegistrationCount = s.SessionRegistrations.Count,
                    s.CreatedAt,
                    s.MaxPlayers,
                    s.RegistrationOpenDate,
                    s.RegistrationCloseDate,
                    s.EarlyBirdPrice,
                    s.EarlyBirdEndDate,
                    s.RegularPrice,
                })
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all sessions");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpGet("sessions/{id}")]
    public async Task<IActionResult> GetSession(int id)
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var session = await _dbContext
                .Sessions
                .Include(s => s.League)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            return Ok(new
            {
                session.Id,
                session.Name,
                session.StartDate,
                session.EndDate,
                session.Fee,
                session.IsActive,
                session.LeagueId,
                LeagueName = session.League?.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching session {SessionId}", id);
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

            // Auto-activate session only if RegistrationOpenDate has passed
            var now = DateTime.UtcNow;
            var isActive = model.IsActive;

            _logger.LogInformation(
                "Creating session '{SessionName}': Model.IsActive={ModelIsActive}, RegistrationOpenDate={RegOpenDate}, CurrentTime={Now}",
                model.Name,
                model.IsActive,
                model.RegistrationOpenDate,
                now
            );

            if (model.RegistrationOpenDate.HasValue && model.RegistrationOpenDate.Value <= now)
            {
                isActive = true;
                _logger.LogInformation(
                    "Auto-activating session '{SessionName}' because RegistrationOpenDate ({RegOpenDate}) <= CurrentTime ({Now})",
                    model.Name,
                    model.RegistrationOpenDate.Value,
                    now
                );
            }

            var session = new Session
            {
                Name = model.Name,
                Description = model.Description,
                StartDate = model.StartDate,
                StartTime = model.StartTime,
                EndDate = model.EndDate,
                EndTime = model.EndTime,
                Fee = model.RegularPrice ?? model.Fee, // Use RegularPrice as Fee if provided
                IsActive = isActive,
                DraftEnabled = model.DraftEnabled,
                LeagueId = model.LeagueId,
                MaxPlayers = model.MaxPlayers,
                RegistrationOpenDate = model.RegistrationOpenDate,
                RegistrationCloseDate = model.RegistrationCloseDate,
                EarlyBirdPrice = model.EarlyBirdPrice,
                EarlyBirdEndDate = model.EarlyBirdEndDate,
                RegularPrice = model.RegularPrice,
                CreatedAt = DateTime.UtcNow,
            };

            await _sessionRepository.AddAsync(session);
            await _sessionRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Session created: {SessionName} (ID: {SessionId}), Final IsActive={IsActive}",
                session.Name,
                session.Id,
                session.IsActive
            );
            return Ok(
                new
                {
                    message = $"Session \"{session.Name}\" created successfully",
                    sessionId = session.Id,
                }
            );
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

            var session = await _sessionRepository.GetByIdAsync(id);
            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            _logger.LogInformation(
                "Updating session {SessionId}. RegistrationOpenDate: {RegOpen}, RegistrationCloseDate: {RegClose}, EarlyBirdEndDate: {EBEnd}",
                id,
                model.RegistrationOpenDate,
                model.RegistrationCloseDate,
                model.EarlyBirdEndDate
            );

            session.Name = model.Name;
            session.Description = model.Description;
            session.StartDate = model.StartDate;
            session.StartTime = model.StartTime;
            session.EndDate = model.EndDate;
            session.EndTime = model.EndTime;
            session.Fee = model.RegularPrice ?? model.Fee; // Use RegularPrice as Fee if provided

            // Respect admin's manual status setting
            // Only auto-activate/deactivate if no RegistrationOpenDate is set
            session.IsActive = model.IsActive;
            session.DraftEnabled = model.DraftEnabled;

            session.LeagueId = model.LeagueId;
            session.MaxPlayers = model.MaxPlayers;
            session.RegistrationOpenDate = model.RegistrationOpenDate;
            session.RegistrationCloseDate = model.RegistrationCloseDate;
            session.EarlyBirdPrice = model.EarlyBirdPrice;
            session.EarlyBirdEndDate = model.EarlyBirdEndDate;
            session.RegularPrice = model.RegularPrice;
            session.LastModified = DateTime.UtcNow;

            await _sessionRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Session updated: {SessionName} (ID: {SessionId})",
                session.Name,
                session.Id
            );
            return Ok(new { message = $"Session \"{session.Name}\" updated successfully" });
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

            var session = await _sessionRepository.GetByIdWithRegistrationsAsync(id);

            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            // Check if there are any registrations
            if (session.SessionRegistrations.Any())
            {
                // Instead of deleting, deactivate the session
                session.IsActive = false;
                await _sessionRepository.SaveChangesAsync();
                _logger.LogInformation(
                    "Session deactivated (has registrations): {SessionName} (ID: {SessionId})",
                    session.Name,
                    session.Id
                );
                return Ok(new { message = "Session deactivated (has existing registrations)" });
            }

            _sessionRepository.Remove(session);
            await _sessionRepository.SaveChangesAsync();

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

    // Get all registrations for a specific session
    [HttpGet("sessions/{id}/registrations")]
    public async Task<IActionResult> GetSessionRegistrations(int id)
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var session = await _sessionRepository.GetByIdWithRegistrationsAsync(id);

            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            // Get player assignments with team and jersey info
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

    // Manually add a user to a session
    [HttpPost("sessions/{id}/registrations/manual")]
    public async Task<IActionResult> AddManualRegistration(int id, [FromBody] ManualRegistrationModel model)
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

            var session = await _sessionRepository.GetByIdWithRegistrationsAsync(id);

            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            // Check capacity
            if (session.SessionRegistrations.Count >= session.MaxPlayers)
            {
                return BadRequest(new { message = "Session is at full capacity" });
            }

            // Check for duplicate registration by email
            if (session.SessionRegistrations.Any(r => r.Email.ToLower() == model.Email.ToLower()))
            {
                return Conflict(new { message = "User is already registered for this session" });
            }

            // Find user by email if exists
            ApplicationUser? user = null;
            if (!string.IsNullOrEmpty(model.Email))
            {
                user = await _userManager.FindByEmailAsync(model.Email);

                // If user doesn't exist, create one for manual registration
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
                    {
                        return BadRequest(new { message = "Failed to create user account", errors = result.Errors });
                    }

                    _logger.LogInformation(
                        "Created manual registration user account for {Email} with setup token {Token}",
                        user.Email,
                        user.PasswordSetupToken
                    );
                }
            }

            // Create registration
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
            await _registrationRepository.SaveChangesAsync(); // Save to get registration.Id

            // Create payment record
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
                model.Name,
                model.Email,
                id
            );

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

    // Update a registration
    [HttpPut("sessions/{sessionId}/registrations/{registrationId}")]
    public async Task<IActionResult> UpdateRegistration(int sessionId, int registrationId, [FromBody] ManualRegistrationModel model)
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

            var registration = await _registrationRepository.GetByIdAndSessionAsync(registrationId, sessionId);

            if (registration == null)
            {
                return NotFound(new { message = "Registration not found" });
            }

            // Update registration fields
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

            // Update associated payment amount if exists
            var payment = await _paymentRepository.GetByRegistrationIdAsync(registrationId);

            if (payment != null)
            {
                payment.Amount = model.AmountPaid;
            }

            // Update jersey number if player is assigned to a team
            if (model.JerseyNumber.HasValue)
            {
                var player = await _dbContext.Players
                    .FirstOrDefaultAsync(p => p.SessionRegistrationId == registrationId);

                if (player != null)
                {
                    // Check for jersey number conflicts within the same team
                    var conflictingPlayer = await _dbContext.Players
                        .FirstOrDefaultAsync(p => p.TeamId == player.TeamId &&
                                                  p.JerseyNumber == model.JerseyNumber &&
                                                  p.Id != player.Id);

                    if (conflictingPlayer != null)
                    {
                        return BadRequest(new { message = $"Jersey number {model.JerseyNumber} is already assigned to another player on this team" });
                    }

                    player.JerseyNumber = model.JerseyNumber;
                    player.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _registrationRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Admin updated registration {RegistrationId} for session {SessionId}",
                registrationId,
                sessionId
            );

            return Ok(new { message = $"Registration for {model.Name} updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating registration");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    // Remove a user from a session
    [HttpDelete("sessions/{sessionId}/registrations/{registrationId}")]
    public async Task<IActionResult> RemoveRegistration(int sessionId, int registrationId)
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var registration = await _registrationRepository.GetByIdAndSessionAsync(registrationId, sessionId);

            if (registration == null)
            {
                return NotFound(new { message = "Registration not found" });
            }

            // Find and delete associated payment first (due to foreign key constraint)
            var payment = await _paymentRepository.GetByRegistrationIdAsync(registrationId);

            if (payment != null)
            {
                _paymentRepository.Remove(payment);
            }

            // Then delete the registration
            _registrationRepository.Remove(registration);

            await _registrationRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Admin removed registration {RegistrationId} ({Name}) from session {SessionId}",
                registrationId,
                registration.Name,
                sessionId
            );

            return Ok(new { message = $"{registration.Name} removed from session. Note: Refunds must be processed manually." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing registration");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    // Get admin dashboard analytics
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardAnalytics()
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            // Get today's registrations
            var todaysRegistrations = await _registrationRepository.GetByDateRangeAsync(today, tomorrow);

            // Get all active sessions with registration details (sessions that haven't ended yet)
            var now = DateTime.UtcNow;

            var activeSessionEntities = await _sessionRepository.GetActiveSessionsWithDetailsAsync(now);
            var activeSessions = activeSessionEntities
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    LeagueName = s.League != null ? s.League.Name : null,
                    s.StartDate,
                    s.EndDate,
                    s.MaxPlayers,
                    RegisteredCount = s.SessionRegistrations.Count,
                    SpotsRemaining = s.MaxPlayers - s.SessionRegistrations.Count,
                    TotalRevenue = s.SessionRegistrations.Sum(r => r.AmountPaid),
                    s.RegularPrice
                })
                .ToList();

            // Calculate overall revenue
            var totalRevenue = await _registrationRepository.GetTotalRevenueAsync();

            var thisMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var monthRevenue = await _registrationRepository.GetRevenueFromDateAsync(thisMonthStart);

            // Get upcoming sessions (next 7 days)
            var nextWeek = DateTime.UtcNow.AddDays(7);
            var upcomingSessionEntities = await _sessionRepository.GetUpcomingAsync(DateTime.UtcNow, nextWeek);
            var upcomingSessions = upcomingSessionEntities
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    LeagueName = s.League != null ? s.League.Name : null,
                    s.StartDate,
                    s.EndDate,
                    RegisteredCount = s.SessionRegistrations.Count,
                    s.MaxPlayers
                })
                .ToList();

            return Ok(new
            {
                todaysRegistrationsCount = todaysRegistrations.Count,
                activeSessionsCount = activeSessions.Count,
                totalRevenue,
                monthRevenue,
                activeSessions,
                upcomingSessions,
                recentRegistrations = todaysRegistrations
                    .OrderByDescending(r => r.RegistrationDate)
                    .Take(10)
                    .Select(r => new
                    {
                        r.Id,
                        r.Name,
                        r.Email,
                        r.SessionId,
                        r.RegistrationDate,
                        r.AmountPaid
                    })
                    .ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard analytics");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpGet("leagues")]
    public async Task<IActionResult> GetAllLeagues()
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var leagueEntities = await _leagueRepository.GetAllWithTeamsAsync();
            var leagues = leagueEntities
                .Select(l => new
                {
                    l.Id,
                    l.Name,
                    l.Description,
                    l.StartDate,
                    l.EarlyBirdPrice,
                    l.EarlyBirdEndDate,
                    l.RegularPrice,
                    l.RegistrationOpenDate,
                    l.RegistrationCloseDate,
                    TeamCount = l.Teams != null ? l.Teams.Count : 0,
                })
                .ToList();

            return Ok(leagues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all leagues");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPut("leagues/{id}")]
    public async Task<IActionResult> UpdateLeague(int id, [FromBody] UpdateLeagueModel model)
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

            var league = await _leagueRepository.GetByIdAsync(id);
            if (league == null)
            {
                return NotFound(new { message = "League not found" });
            }

            _logger.LogInformation(
                "Updating league {LeagueId}. StartDate: {StartDate}, RegistrationOpenDate: {RegOpen}, RegistrationCloseDate: {RegClose}",
                id,
                model.StartDate,
                model.RegistrationOpenDate,
                model.RegistrationCloseDate
            );

            league.Name = model.Name;
            league.Description = model.Description;
            league.StartDate = model.StartDate;
            league.EarlyBirdPrice = model.EarlyBirdPrice;
            league.EarlyBirdEndDate = model.EarlyBirdEndDate;
            league.RegularPrice = model.RegularPrice;
            league.RegistrationOpenDate = model.RegistrationOpenDate;
            league.RegistrationCloseDate = model.RegistrationCloseDate;

            await _leagueRepository.SaveChangesAsync();

            _logger.LogInformation(
                "League updated: {LeagueName} (ID: {LeagueId})",
                league.Name,
                league.Id
            );
            return Ok(new { message = $"League \"{league.Name}\" updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating league");
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

    /// <summary>
    /// Publish or unpublish a draft to make teams visible to players
    /// </summary>
    [HttpPut("sessions/{sessionId}/publish-draft")]
    public async Task<IActionResult> PublishDraft(int sessionId, [FromBody] PublishDraftModel model)
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }

            var session = await _sessionRepository.GetByIdAsync(sessionId);

            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            if (!session.DraftEnabled)
            {
                return BadRequest(new { message = "Draft is not enabled for this session" });
            }

            session.DraftPublished = model.Published;
            await _sessionRepository.SaveChangesAsync();

            var status = model.Published ? "published" : "unpublished";
            _logger.LogInformation("Draft for session {SessionId} {Status}", sessionId, status);

            return Ok(new
            {
                message = $"Draft successfully {status}",
                sessionId = session.Id,
                draftPublished = session.DraftPublished
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing draft for session {SessionId}", sessionId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPut("users/{userId}/rating")]
    public async Task<IActionResult> UpdatePlayerRating(string userId, [FromBody] UpdatePlayerRatingModel model)
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

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Update rating and notes
            user.Rating = model.Rating;
            user.PlayerNotes = model.PlayerNotes;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Failed to update user", errors = result.Errors });
            }

            return Ok(new
            {
                message = "Player rating and notes updated successfully",
                userId = user.Id,
                rating = user.Rating,
                playerNotes = user.PlayerNotes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating player rating for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPut("users/{userId}/profile")]
    public async Task<IActionResult> UpdateUserProfile(string userId, [FromBody] UpdateUserProfileModel model)
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

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Update user profile fields
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Address = model.Address;
            user.City = model.City;
            user.State = model.State;
            user.ZipCode = model.ZipCode;
            user.Phone = model.Phone;
            user.DateOfBirth = model.DateOfBirth;
            user.Position = model.Position;
            user.Rating = model.Rating;
            user.PlayerNotes = model.PlayerNotes;
            user.LeagueId = model.LeagueId;
            user.UpdatedAt = DateTime.UtcNow;

            // Handle email change separately
            if (user.Email != model.Email)
            {
                var emailExists = await _userManager.FindByEmailAsync(model.Email);
                if (emailExists != null && emailExists.Id != userId)
                {
                    return BadRequest(new { message = "Email address is already in use" });
                }

                var emailToken = await _userManager.GenerateChangeEmailTokenAsync(user, model.Email);
                var emailResult = await _userManager.ChangeEmailAsync(user, model.Email, emailToken);
                if (!emailResult.Succeeded)
                {
                    return BadRequest(new { message = "Failed to update email", errors = emailResult.Errors });
                }

                // Also update username to match new email
                user.UserName = model.Email;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Failed to update user", errors = result.Errors });
            }

            return Ok(new { message = "User profile updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }
}
