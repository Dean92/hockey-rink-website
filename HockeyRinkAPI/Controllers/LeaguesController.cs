using HockeyRinkAPI.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HockeyRinkAPI.Controllers;

[ApiController]
[Route("api/leagues")]
public class LeaguesController : ControllerBase
{
    private readonly ILeagueRepository _leagueRepository;
    private readonly ILogger<LeaguesController> _logger;

    public LeaguesController(
        ILeagueRepository leagueRepository,
        ILogger<LeaguesController> logger
    )
    {
        _leagueRepository = leagueRepository;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous] // Allow public access to view leagues
    public async Task<IActionResult> GetLeagues()
    {
        try
        {
            _logger.LogInformation("GetLeagues - Request received (public endpoint)");

            var leagues = await _leagueRepository.GetAllAsync();
            _logger.LogInformation("GetLeagues - Found {Count} leagues", leagues.Count);

            return Ok(leagues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetLeagues - Error occurred");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }
}
