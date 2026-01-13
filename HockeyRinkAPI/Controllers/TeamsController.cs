using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
[Route("api/admin/sessions")]
public class TeamsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TeamsController> _logger;

    public TeamsController(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ILogger<TeamsController> logger
    )
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private async Task<string?> GetUserIdFromTokenAsync(string token)
    {
        try
        {
            var decodedData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = decodedData.Split('|');
            if (parts.Length != 3)
                return null;

            var userId = parts[0];
            var email = parts[1];
            var expiry = DateTime.Parse(parts[2]);

            if (expiry < DateTime.UtcNow)
                return null;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.Email != email)
                return null;

            return userId;
        }
        catch
        {
            return null;
        }
    }

    private async Task<bool> IsAdminAsync()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return false;

        var token = authHeader.Substring("Bearer ".Length).Trim();
        var userId = await GetUserIdFromTokenAsync(token);
        if (userId == null)
            return false;

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return false;

        return await _userManager.IsInRoleAsync(user, "Admin");
    }

    [HttpGet("{sessionId}/teams")]
    public async Task<IActionResult> GetTeamsForSession(int sessionId)
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Unauthorized(new { error = "Unauthorized" });
            }

            var session = await _dbContext.Sessions.FindAsync(sessionId);
            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            var teams = await _dbContext.Teams
                .Include(t => t.Players)
                .Where(t => t.SessionId == sessionId)
                .OrderBy(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Id,
                    t.TeamName,
                    t.CaptainName,
                    t.TeamColor,
                    t.MaxPlayers,
                    t.CreatedAt,
                    PlayerCount = t.Players.Count
                })
                .ToListAsync();

            return Ok(teams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch teams for session {SessionId}", sessionId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPost("{sessionId}/teams")]
    public async Task<IActionResult> CreateTeam(int sessionId, [FromBody] CreateTeamModel model)
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Unauthorized(new { error = "Unauthorized" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var session = await _dbContext.Sessions.FindAsync(sessionId);
            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            // Check for duplicate team name in session
            var existingTeam = await _dbContext.Teams
                .FirstOrDefaultAsync(t => t.SessionId == sessionId && t.TeamName == model.TeamName);

            if (existingTeam != null)
            {
                return BadRequest(new { message = "A team with this name already exists for this session" });
            }

            var team = new Team
            {
                SessionId = sessionId,
                TeamName = model.TeamName,
                CaptainName = model.CaptainName,
                TeamColor = model.TeamColor,
                MaxPlayers = model.MaxPlayers,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Teams.Add(team);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Team '{TeamName}' created for session {SessionId}", model.TeamName, sessionId);

            return Ok(new
            {
                message = "Team created successfully",
                team = new
                {
                    team.Id,
                    team.TeamName,
                    team.CaptainName,
                    team.TeamColor,
                    team.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create team for session {SessionId}", sessionId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPut("teams/{teamId}")]
    public async Task<IActionResult> UpdateTeam(int teamId, [FromBody] UpdateTeamModel model)
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Unauthorized(new { error = "Unauthorized" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var team = await _dbContext.Teams.FindAsync(teamId);
            if (team == null)
            {
                return NotFound(new { message = "Team not found" });
            }

            // Check for duplicate team name in session (excluding current team)
            var existingTeam = await _dbContext.Teams
                .FirstOrDefaultAsync(t => t.SessionId == team.SessionId &&
                                         t.TeamName == model.TeamName &&
                                         t.Id != teamId);

            if (existingTeam != null)
            {
                return BadRequest(new { message = "A team with this name already exists for this session" });
            }

            team.TeamName = model.TeamName;
            team.CaptainName = model.CaptainName;
            team.CaptainUserId = model.CaptainUserId;
            team.TeamColor = model.TeamColor;
            team.MaxPlayers = model.MaxPlayers;
            team.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Team {TeamId} updated", teamId);

            return Ok(new
            {
                message = "Team updated successfully",
                team = new
                {
                    team.Id,
                    team.TeamName,
                    team.CaptainName,
                    team.TeamColor,
                    team.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update team {TeamId}", teamId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpDelete("teams/{teamId}")]
    public async Task<IActionResult> DeleteTeam(int teamId)
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Unauthorized(new { error = "Unauthorized" });
            }

            var team = await _dbContext.Teams.FindAsync(teamId);
            if (team == null)
            {
                return NotFound(new { message = "Team not found" });
            }

            _dbContext.Teams.Remove(team);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Team {TeamId} deleted", teamId);

            return Ok(new { message = "Team deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete team {TeamId}", teamId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    // Get players available for draft (session registrations with ratings)
    [HttpGet("{sessionId}/draft/players")]
    public async Task<IActionResult> GetDraftPlayers(int sessionId)
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Unauthorized(new { error = "Unauthorized" });
            }

            var session = await _dbContext.Sessions.FindAsync(sessionId);
            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            // Get all registered players for this session
            var players = await _dbContext.SessionRegistrations
                .Where(sr => sr.SessionId == sessionId)
                .OrderByDescending(sr => sr.Rating ?? 0) // Sort by rating (highest first), nulls at bottom
                .ThenBy(sr => sr.Name)
                .Select(sr => new
                {
                    sr.Id,
                    sr.Name,
                    sr.Position,
                    sr.Rating,
                    sr.Email,
                    // Check if player is already assigned to a team
                    TeamId = _dbContext.Players
                        .Where(p => p.SessionRegistrationId == sr.Id)
                        .Select(p => (int?)p.TeamId)
                        .FirstOrDefault(),
                    TeamName = _dbContext.Players
                        .Where(p => p.SessionRegistrationId == sr.Id)
                        .Select(p => p.Team.TeamName)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(players);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch draft players for session {SessionId}", sessionId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    // Set player rating
    [HttpPut("{sessionId}/registrations/{registrationId}/rating")]
    public async Task<IActionResult> SetPlayerRating(int sessionId, int registrationId, [FromBody] SetRatingModel model)
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Unauthorized(new { error = "Unauthorized" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var registration = await _dbContext.SessionRegistrations
                .FirstOrDefaultAsync(sr => sr.Id == registrationId && sr.SessionId == sessionId);

            if (registration == null)
            {
                return NotFound(new { message = "Registration not found" });
            }

            registration.Rating = model.Rating;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Set rating {Rating} for registration {RegistrationId}", model.Rating, registrationId);

            return Ok(new { message = "Rating updated successfully", rating = registration.Rating });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set rating for registration {RegistrationId}", registrationId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    // Assign player to team
    [HttpPost("{sessionId}/teams/{teamId}/players")]
    public async Task<IActionResult> AssignPlayerToTeam(int sessionId, int teamId, [FromBody] AssignPlayerModel model)
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Unauthorized(new { error = "Unauthorized" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var team = await _dbContext.Teams
                .FirstOrDefaultAsync(t => t.Id == teamId && t.SessionId == sessionId);

            if (team == null)
            {
                return NotFound(new { message = "Team not found" });
            }

            var registration = await _dbContext.SessionRegistrations
                .FirstOrDefaultAsync(sr => sr.Id == model.RegistrationId && sr.SessionId == sessionId);

            if (registration == null)
            {
                return NotFound(new { message = "Registration not found" });
            }

            // Check if player is already assigned to a team in this session
            var existingAssignment = await _dbContext.Players
                .FirstOrDefaultAsync(p => p.SessionRegistrationId == model.RegistrationId);

            if (existingAssignment != null)
            {
                return BadRequest(new { message = "Player is already assigned to a team" });
            }

            // Create player assignment
            var player = new Player
            {
                UserId = registration.UserId,
                TeamId = teamId,
                SessionRegistrationId = registration.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Players.Add(player);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Assigned player {RegistrationId} to team {TeamId}", model.RegistrationId, teamId);

            return Ok(new
            {
                message = "Player assigned to team successfully",
                player = new
                {
                    player.Id,
                    registration.Name,
                    registration.Position,
                    TeamId = teamId,
                    team.TeamName
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign player to team {TeamId}", teamId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    // Remove player from team
    [HttpDelete("{sessionId}/teams/{teamId}/players/{registrationId}")]
    public async Task<IActionResult> RemovePlayerFromTeam(int sessionId, int teamId, int registrationId)
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Unauthorized(new { error = "Unauthorized" });
            }

            var player = await _dbContext.Players
                .Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.SessionRegistrationId == registrationId && p.TeamId == teamId && p.Team.SessionId == sessionId);

            if (player == null)
            {
                return NotFound(new { message = "Player assignment not found" });
            }

            _dbContext.Players.Remove(player);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Removed player {RegistrationId} from team {TeamId}", registrationId, teamId);

            return Ok(new { message = "Player removed from team successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove player from team {TeamId}", teamId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    // Get team with players
    [HttpGet("{sessionId}/teams/{teamId}/players")]
    public async Task<IActionResult> GetTeamPlayers(int sessionId, int teamId)
    {
        try
        {
            if (!await IsAdminAsync())
            {
                return Unauthorized(new { error = "Unauthorized" });
            }

            var team = await _dbContext.Teams
                .Include(t => t.Players)
                .ThenInclude(p => p.SessionRegistration)
                .FirstOrDefaultAsync(t => t.Id == teamId && t.SessionId == sessionId);

            if (team == null)
            {
                return NotFound(new { message = "Team not found" });
            }

            var players = team.Players.Select(p => new
            {
                RegistrationId = p.SessionRegistrationId,
                p.SessionRegistration.Name,
                p.SessionRegistration.Position,
                p.SessionRegistration.Rating
            }).ToList();

            return Ok(new
            {
                team.Id,
                team.TeamName,
                team.CaptainName,
                team.TeamColor,
                PlayerCount = players.Count,
                Players = players
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch team players for team {TeamId}", teamId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }
}

public class SetRatingModel
{
    [Range(0.0, 10.0)]
    public decimal? Rating { get; set; }
}

public class AssignPlayerModel
{
    [Required]
    public int RegistrationId { get; set; }
}

public class CreateTeamModel
{
    [Required]
    [StringLength(100)]
    public required string TeamName { get; set; }

    [StringLength(100)]
    public string? CaptainName { get; set; }

    [StringLength(20)]
    public string? TeamColor { get; set; }

    [Range(1, 50)]
    public int? MaxPlayers { get; set; }
}

public class UpdateTeamModel
{
    [Required]
    [StringLength(100)]
    public required string TeamName { get; set; }

    [StringLength(100)]
    public string? CaptainName { get; set; }

    public string? CaptainUserId { get; set; }

    [StringLength(20)]
    public string? TeamColor { get; set; }

    [Range(1, 50)]
    public int? MaxPlayers { get; set; }
}
