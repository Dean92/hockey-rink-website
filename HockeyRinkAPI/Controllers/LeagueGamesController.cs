using HockeyRinkAPI.Models.Responses;
using HockeyRinkAPI.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HockeyRinkAPI.Controllers;

/// <summary>
/// Public (no auth required) endpoint for reading league game schedules.
/// </summary>
[ApiController]
[Route("api/leagues")]
public class LeagueGamesController : ControllerBase
{
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<LeagueGamesController> _logger;

    public LeagueGamesController(IGameRepository gameRepository, ILogger<LeagueGamesController> logger)
    {
        _gameRepository = gameRepository;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/leagues/{id}/games
    /// Returns all Scheduled and Completed games for a league (public read-only).
    /// </summary>
    [HttpGet("{leagueId}/games")]
    public async Task<IActionResult> GetLeagueGames(
        int leagueId,
        [FromQuery] int? teamId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? rinkId = null)
    {
        try
        {
            // Public view — only expose Scheduled and Completed games
            var allGames = await _gameRepository.GetLeagueGamesAsync(leagueId, teamId, null, startDate, endDate, rinkId);
            var publicGames = allGames
                .Where(g => g.Status == "Scheduled" || g.Status == "Completed")
                .Select(g => new GameSummaryDto
                {
                    Id = g.Id,
                    GameDate = g.GameDate,
                    SessionId = g.SessionId,
                    SessionName = g.Session?.Name ?? string.Empty,
                    RinkId = g.RinkId,
                    RinkName = g.Rink?.Name,
                    HomeTeamId = g.HomeTeamId,
                    HomeTeamName = g.HomeTeam?.TeamName ?? string.Empty,
                    AwayTeamId = g.AwayTeamId,
                    AwayTeamName = g.AwayTeam?.TeamName ?? string.Empty,
                    HomeScore = g.HomeScore,
                    AwayScore = g.AwayScore,
                    Status = g.Status,
                    Location = g.Location
                })
                .ToList();

            return Ok(publicGames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching public games for league {LeagueId}", leagueId);
            return StatusCode(500, new { error = "Internal Server Error" });
        }
    }
}
