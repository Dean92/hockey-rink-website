using System;
using System.ComponentModel.DataAnnotations;
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
                .ToListAsync();

            // Auto-activate/deactivate sessions based on dates
            var now = DateTime.UtcNow;
            bool hasChanges = false;
            foreach (var session in sessions)
            {
                _logger.LogDebug(
                    "Checking session {SessionId} '{SessionName}': IsActive={IsActive}, RegOpenDate={RegOpenDate}, Now={Now}, LastModified={LastModified}",
                    session.Id,
                    session.Name,
                    session.IsActive,
                    session.RegistrationOpenDate,
                    now,
                    session.LastModified
                );

                // Auto-activate if registration open date has passed and session is inactive
                if (session.RegistrationOpenDate.HasValue &&
                    session.RegistrationOpenDate.Value <= now &&
                    !session.IsActive)
                {
                    // Auto-activate unless the admin manually deactivated it AFTER the registration opened
                    bool manuallyDeactivatedAfterOpen = session.LastModified.HasValue &&
                                                       session.LastModified.Value > session.RegistrationOpenDate.Value;

                    _logger.LogInformation(
                        "Session {SessionId} eligible for auto-activation. ManuallyDeactivatedAfterOpen={ManuallyDeactivated}",
                        session.Id,
                        manuallyDeactivatedAfterOpen
                    );

                    if (!manuallyDeactivatedAfterOpen)
                    {
                        session.IsActive = true;
                        hasChanges = true;
                        _logger.LogInformation(
                            "Auto-activated session: {SessionName} (ID: {SessionId}) - Registration opened at: {OpenDate}, Current time: {Now}",
                            session.Name,
                            session.Id,
                            session.RegistrationOpenDate,
                            now
                        );
                    }
                }

                // Auto-deactivate sessions where dates have passed
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

            var result = sessions
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
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Fee = model.RegularPrice ?? model.Fee, // Use RegularPrice as Fee if provided
                IsActive = isActive,
                LeagueId = model.LeagueId,
                MaxPlayers = model.MaxPlayers,
                RegistrationOpenDate = model.RegistrationOpenDate,
                RegistrationCloseDate = model.RegistrationCloseDate,
                EarlyBirdPrice = model.EarlyBirdPrice,
                EarlyBirdEndDate = model.EarlyBirdEndDate,
                RegularPrice = model.RegularPrice,
                CreatedAt = DateTime.UtcNow,
            };

            _dbContext.Sessions.Add(session);
            await _dbContext.SaveChangesAsync();

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

            var session = await _dbContext.Sessions.FindAsync(id);
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
            session.StartDate = model.StartDate;
            session.EndDate = model.EndDate;
            session.Fee = model.RegularPrice ?? model.Fee; // Use RegularPrice as Fee if provided

            // Respect admin's manual status setting
            // Only auto-activate/deactivate if no RegistrationOpenDate is set
            session.IsActive = model.IsActive;

            session.LeagueId = model.LeagueId;
            session.MaxPlayers = model.MaxPlayers;
            session.RegistrationOpenDate = model.RegistrationOpenDate;
            session.RegistrationCloseDate = model.RegistrationCloseDate;
            session.EarlyBirdPrice = model.EarlyBirdPrice;
            session.EarlyBirdEndDate = model.EarlyBirdEndDate;
            session.RegularPrice = model.RegularPrice;
            session.LastModified = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

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

            var session = await _dbContext
                .Sessions.Include(s => s.Registrations)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            var registrations = session.Registrations.Select(r => new
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
                UserEmail = r.User?.Email
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

            var session = await _dbContext
                .Sessions.Include(s => s.Registrations)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            // Check capacity
            if (session.Registrations.Count >= session.MaxPlayers)
            {
                return BadRequest(new { message = "Session is at full capacity" });
            }

            // Check for duplicate registration by email
            if (session.Registrations.Any(r => r.Email.ToLower() == model.Email.ToLower()))
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
                DateOfBirth = model.DateOfBirth,
                Position = model.Position,
                RegistrationDate = DateTime.UtcNow,
                AmountPaid = model.AmountPaid
            };

            _dbContext.SessionRegistrations.Add(registration);
            await _dbContext.SaveChangesAsync(); // Save to get registration.Id

            // Create payment record
            var payment = new Payment
            {
                SessionRegistrationId = registration.Id,
                Amount = model.AmountPaid,
                TransactionId = $"MANUAL-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Status = "Completed"
            };

            _dbContext.Payments.Add(payment);
            await _dbContext.SaveChangesAsync();

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

            var registration = await _dbContext
                .SessionRegistrations
                .Include(r => r.Session)
                .FirstOrDefaultAsync(r => r.Id == registrationId && r.SessionId == sessionId);

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
            registration.DateOfBirth = model.DateOfBirth;
            registration.Position = model.Position;
            registration.AmountPaid = model.AmountPaid;

            // Update associated payment amount if exists
            var payment = await _dbContext
                .Payments
                .FirstOrDefaultAsync(p => p.SessionRegistrationId == registrationId);

            if (payment != null)
            {
                payment.Amount = model.AmountPaid;
            }

            await _dbContext.SaveChangesAsync();

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

            var registration = await _dbContext
                .SessionRegistrations
                .Include(r => r.Session)
                .FirstOrDefaultAsync(r => r.Id == registrationId && r.SessionId == sessionId);

            if (registration == null)
            {
                return NotFound(new { message = "Registration not found" });
            }

            // Find and delete associated payment first (due to foreign key constraint)
            var payment = await _dbContext
                .Payments
                .FirstOrDefaultAsync(p => p.SessionRegistrationId == registrationId);

            if (payment != null)
            {
                _dbContext.Payments.Remove(payment);
            }

            // Then delete the registration
            _dbContext.SessionRegistrations.Remove(registration);

            await _dbContext.SaveChangesAsync();

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
            var todaysRegistrations = await _dbContext.SessionRegistrations
                .Where(r => r.RegistrationDate >= today && r.RegistrationDate < tomorrow)
                .ToListAsync();

            // Get all active sessions with registration details
            var activeSessions = await _dbContext.Sessions
                .Include(s => s.League)
                .Include(s => s.Registrations)
                .Where(s => s.IsActive)
                .OrderBy(s => s.StartDate)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    LeagueName = s.League != null ? s.League.Name : null,
                    s.StartDate,
                    s.EndDate,
                    s.MaxPlayers,
                    RegisteredCount = s.Registrations.Count,
                    SpotsRemaining = s.MaxPlayers - s.Registrations.Count,
                    TotalRevenue = s.Registrations.Sum(r => r.AmountPaid),
                    s.RegularPrice
                })
                .ToListAsync();

            // Calculate overall revenue
            var totalRevenue = await _dbContext.SessionRegistrations
                .SumAsync(r => r.AmountPaid);

            var thisMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var monthRevenue = await _dbContext.SessionRegistrations
                .Where(r => r.RegistrationDate >= thisMonthStart)
                .SumAsync(r => r.AmountPaid);

            // Get total active registrations
            var activeRegistrationsCount = await _dbContext.SessionRegistrations
                .CountAsync(r => r.Session != null && r.Session.IsActive);

            // Get upcoming sessions (next 7 days)
            var nextWeek = DateTime.UtcNow.AddDays(7);
            var upcomingSessions = await _dbContext.Sessions
                .Include(s => s.League)
                .Where(s => s.StartDate >= DateTime.UtcNow && s.StartDate <= nextWeek)
                .OrderBy(s => s.StartDate)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    LeagueName = s.League != null ? s.League.Name : null,
                    s.StartDate,
                    RegisteredCount = s.Registrations.Count,
                    s.MaxPlayers
                })
                .ToListAsync();

            return Ok(new
            {
                todaysRegistrationsCount = todaysRegistrations.Count,
                activeSessionsCount = activeSessions.Count,
                activeRegistrationsCount,
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

            var leagues = await _dbContext
                .Leagues.OrderBy(l => l.Name)
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
                .ToListAsync();

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

            var league = await _dbContext.Leagues.FindAsync(id);
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

            await _dbContext.SaveChangesAsync();

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
    public bool IsActive { get; set; } = false;
    public int LeagueId { get; set; }
    public int MaxPlayers { get; set; } = 20;
    public DateTime? RegistrationOpenDate { get; set; }
    public DateTime? RegistrationCloseDate { get; set; }
    public decimal? EarlyBirdPrice { get; set; }
    public DateTime? EarlyBirdEndDate { get; set; }
    public decimal? RegularPrice { get; set; }
}

public class UpdateSessionModel
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Fee { get; set; }
    public bool IsActive { get; set; }
    public int LeagueId { get; set; }
    public int MaxPlayers { get; set; } = 20;
    public DateTime? RegistrationOpenDate { get; set; }
    public DateTime? RegistrationCloseDate { get; set; }
    public decimal? EarlyBirdPrice { get; set; }
    public DateTime? EarlyBirdEndDate { get; set; }
    public decimal? RegularPrice { get; set; }
}

public class UpdateLeagueModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public decimal? EarlyBirdPrice { get; set; }
    public DateTime? EarlyBirdEndDate { get; set; }
    public decimal? RegularPrice { get; set; }
    public DateTime? RegistrationOpenDate { get; set; }
    public DateTime? RegistrationCloseDate { get; set; }
}

public class ManualRegistrationModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(50)]
    public string? State { get; set; }

    [StringLength(20)]
    public string? ZipCode { get; set; }

    [Required]
    public DateTime DateOfBirth { get; set; }

    [StringLength(20)]
    public string? Position { get; set; }

    [Required]
    [Range(0, 10000)]
    public decimal AmountPaid { get; set; }
}
