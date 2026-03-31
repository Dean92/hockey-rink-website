using HockeyRinkAPI.Models;
using HockeyRinkAPI.Models.Requests;
using HockeyRinkAPI.Repositories;
using HockeyRinkAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HockeyRinkAPI.Controllers.Admin;

[Route("api/admin")]
public class AdminSessionsController : AdminControllerBase
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ISessionActivationService _sessionActivationService;
    private readonly ILogger<AdminSessionsController> _logger;

    public AdminSessionsController(
        ITokenService tokenService,
        UserManager<ApplicationUser> userManager,
        ISessionRepository sessionRepository,
        ISessionActivationService sessionActivationService,
        ILogger<AdminSessionsController> logger)
        : base(tokenService, userManager)
    {
        _sessionRepository = sessionRepository;
        _sessionActivationService = sessionActivationService;
        _logger = logger;
    }

    [HttpGet("sessions/all")]
    public async Task<IActionResult> GetAllSessions()
    {
        try
        {
            if (!await IsAdminAsync())
                return Forbid();

            var sessions = await _sessionRepository.GetAllWithDetailsAsync();

            var hasChanges = await _sessionActivationService.ApplyActivationRulesAsync(sessions);
            if (hasChanges)
                await _sessionRepository.SaveChangesAsync();

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
                    s.RegularSeasonGames,
                    s.GoaliePrice,
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
                return Forbid();

            var session = await _sessionRepository.GetByIdWithLeagueAsync(id);
            if (session == null)
                return NotFound(new { message = "Session not found" });

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
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var now = DateTime.UtcNow;
            var isActive = model.IsActive;

            _logger.LogInformation(
                "Creating session '{SessionName}': Model.IsActive={ModelIsActive}, RegistrationOpenDate={RegOpenDate}, CurrentTime={Now}",
                model.Name, model.IsActive, model.RegistrationOpenDate, now);

            if (model.RegistrationOpenDate.HasValue && model.RegistrationOpenDate.Value <= now)
            {
                isActive = true;
                _logger.LogInformation(
                    "Auto-activating session '{SessionName}' because RegistrationOpenDate ({RegOpenDate}) <= CurrentTime ({Now})",
                    model.Name, model.RegistrationOpenDate.Value, now);
            }

            var session = new Session
            {
                Name = model.Name,
                Description = model.Description,
                StartDate = model.StartDate,
                StartTime = model.StartTime,
                EndDate = model.EndDate,
                EndTime = model.EndTime,
                Fee = model.RegularPrice ?? model.Fee,
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
                RegularSeasonGames = model.RegularSeasonGames,
                GoaliePrice = model.GoaliePrice,
            };

            await _sessionRepository.AddAsync(session);
            await _sessionRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Session created: {SessionName} (ID: {SessionId}), Final IsActive={IsActive}",
                session.Name, session.Id, session.IsActive);

            return Ok(new
            {
                message = $"Session \"{session.Name}\" created successfully",
                sessionId = session.Id,
            });
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
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var session = await _sessionRepository.GetByIdAsync(id);
            if (session == null)
                return NotFound(new { message = "Session not found" });

            _logger.LogInformation(
                "Updating session {SessionId}. RegistrationOpenDate: {RegOpen}, RegistrationCloseDate: {RegClose}, EarlyBirdEndDate: {EBEnd}",
                id, model.RegistrationOpenDate, model.RegistrationCloseDate, model.EarlyBirdEndDate);

            session.Name = model.Name;
            session.Description = model.Description;
            session.StartDate = model.StartDate;
            session.StartTime = model.StartTime;
            session.EndDate = model.EndDate;
            session.EndTime = model.EndTime;
            session.Fee = model.RegularPrice ?? model.Fee;
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
            session.RegularSeasonGames = model.RegularSeasonGames;
            session.GoaliePrice = model.GoaliePrice;

            await _sessionRepository.SaveChangesAsync();

            _logger.LogInformation("Session updated: {SessionName} (ID: {SessionId})", session.Name, session.Id);
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
                return Forbid();

            var session = await _sessionRepository.GetByIdWithRegistrationsAsync(id);
            if (session == null)
                return NotFound(new { message = "Session not found" });

            if (session.SessionRegistrations.Any())
            {
                session.IsActive = false;
                await _sessionRepository.SaveChangesAsync();
                _logger.LogInformation(
                    "Session deactivated (has registrations): {SessionName} (ID: {SessionId})",
                    session.Name, session.Id);
                return Ok(new { message = "Session deactivated (has existing registrations)" });
            }

            _sessionRepository.Remove(session);
            await _sessionRepository.SaveChangesAsync();

            _logger.LogInformation("Session deleted: {SessionName} (ID: {SessionId})", session.Name, session.Id);
            return Ok(new { message = "Session deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPut("sessions/{sessionId}/publish-draft")]
    public async Task<IActionResult> PublishDraft(int sessionId, [FromBody] PublishDraftModel model)
    {
        try
        {
            if (!await IsAdminAsync())
                return Forbid();

            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session == null)
                return NotFound(new { message = "Session not found" });

            if (!session.DraftEnabled)
                return BadRequest(new { message = "Draft is not enabled for this session" });

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
}
