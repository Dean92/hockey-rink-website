using HockeyRinkAPI.Models;
using HockeyRinkAPI.Models.Requests;
using HockeyRinkAPI.Repositories;
using HockeyRinkAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HockeyRinkAPI.Controllers.Admin;

[Route("api/admin")]
public class AdminLeaguesController : AdminControllerBase
{
    private readonly ILeagueRepository _leagueRepository;
    private readonly ILogger<AdminLeaguesController> _logger;

    public AdminLeaguesController(
        ITokenService tokenService,
        UserManager<ApplicationUser> userManager,
        ILeagueRepository leagueRepository,
        ILogger<AdminLeaguesController> logger)
        : base(tokenService, userManager)
    {
        _leagueRepository = leagueRepository;
        _logger = logger;
    }

    [HttpGet("leagues")]
    public async Task<IActionResult> GetAllLeagues()
    {
        try
        {
            if (!await IsAdminAsync())
                return Forbid();

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
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var league = await _leagueRepository.GetByIdAsync(id);
            if (league == null)
                return NotFound(new { message = "League not found" });

            _logger.LogInformation(
                "Updating league {LeagueId}. StartDate: {StartDate}, RegistrationOpenDate: {RegOpen}, RegistrationCloseDate: {RegClose}",
                id, model.StartDate, model.RegistrationOpenDate, model.RegistrationCloseDate);

            league.Name = model.Name;
            league.Description = model.Description;
            league.StartDate = model.StartDate;
            league.EarlyBirdPrice = model.EarlyBirdPrice;
            league.EarlyBirdEndDate = model.EarlyBirdEndDate;
            league.RegularPrice = model.RegularPrice;
            league.RegistrationOpenDate = model.RegistrationOpenDate;
            league.RegistrationCloseDate = model.RegistrationCloseDate;

            await _leagueRepository.SaveChangesAsync();

            _logger.LogInformation("League updated: {LeagueName} (ID: {LeagueId})", league.Name, league.Id);
            return Ok(new { message = $"League \"{league.Name}\" updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating league");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPost("leagues")]
    public async Task<IActionResult> CreateLeague([FromBody] UpdateLeagueModel model)
    {
        try
        {
            if (!await IsAdminAsync())
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var league = new League
            {
                Name = model.Name,
                Description = model.Description,
                StartDate = model.StartDate,
                EarlyBirdPrice = model.EarlyBirdPrice,
                EarlyBirdEndDate = model.EarlyBirdEndDate,
                RegularPrice = model.RegularPrice,
                RegistrationOpenDate = model.RegistrationOpenDate,
                RegistrationCloseDate = model.RegistrationCloseDate,
                CreatedAt = DateTime.Now,
            };

            await _leagueRepository.AddAsync(league);
            await _leagueRepository.SaveChangesAsync();

            _logger.LogInformation("League created: {LeagueName} (ID: {LeagueId})", league.Name, league.Id);
            return Ok(new { message = $"League \"{league.Name}\" created successfully", id = league.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating league");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpDelete("leagues/{id}")]
    public async Task<IActionResult> DeleteLeague(int id)
    {
        try
        {
            if (!await IsAdminAsync())
                return Forbid();

            var league = await _leagueRepository.GetByIdWithTeamsAsync(id);
            if (league == null)
                return NotFound(new { message = "League not found" });

            if (league.Teams != null && league.Teams.Count > 0)
                return Conflict(new { message = $"Cannot delete league \"{league.Name}\" because it has {league.Teams.Count} team(s). Remove all teams first." });

            await _leagueRepository.NullifySessionLeagueIdAsync(id);
            await _leagueRepository.DeleteAsync(league);
            await _leagueRepository.SaveChangesAsync();

            _logger.LogInformation("League deleted: {LeagueName} (ID: {LeagueId})", league.Name, id);
            return Ok(new { message = $"League \"{league.Name}\" deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting league");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }
}
