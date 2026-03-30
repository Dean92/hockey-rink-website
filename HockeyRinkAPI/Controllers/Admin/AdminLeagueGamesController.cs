using HockeyRinkAPI.Models;
using HockeyRinkAPI.Models.Requests;
using HockeyRinkAPI.Models.Responses;
using HockeyRinkAPI.Repositories;
using HockeyRinkAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HockeyRinkAPI.Controllers.Admin;

[Route("api/admin")]
public class AdminLeagueGamesController : AdminControllerBase
{
    private readonly IGameRepository _gameRepository;
    private readonly IConflictDetectionService _conflictService;
    private readonly ILogger<AdminLeagueGamesController> _logger;

    public AdminLeagueGamesController(
        ITokenService tokenService,
        UserManager<ApplicationUser> userManager,
        IGameRepository gameRepository,
        IConflictDetectionService conflictService,
        ILogger<AdminLeagueGamesController> logger)
        : base(tokenService, userManager)
    {
        _gameRepository = gameRepository;
        _conflictService = conflictService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/admin/leagues/{id}/games
    /// Returns all games for a league with optional filters.
    /// </summary>
    [HttpGet("leagues/{leagueId}/games")]
    public async Task<IActionResult> GetLeagueGames(
        int leagueId,
        [FromQuery] int? teamId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? rinkId = null)
    {
        try
        {
            if (!await IsAdminAsync()) return Forbid();

            var games = await _gameRepository.GetLeagueGamesAsync(leagueId, teamId, status, startDate, endDate, rinkId);
            var result = games.Select(MapToDto).ToList();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching games for league {LeagueId}", leagueId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/admin/games/{id}
    /// Updates a game's date, rink, teams, or score. Runs conflict detection on save.
    /// </summary>
    [HttpPut("games/{id}")]
    public async Task<IActionResult> UpdateGame(int id, [FromBody] UpdateGameRequest request)
    {
        try
        {
            if (!await IsAdminAsync()) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var game = await _gameRepository.GetByIdAsync(id);
            if (game == null) return NotFound(new { message = "Game not found" });

            // Conflict check if rink or time changed
            if (request.RinkId.HasValue)
            {
                var gameLengthMinutes = 90;
                var slotEnd = request.GameDate.AddMinutes(gameLengthMinutes);
                var conflict = await _conflictService.CheckAsync(
                    request.RinkId.Value,
                    request.GameDate,
                    slotEnd,
                    excludeGameId: id);

                if (conflict.HasConflict)
                    return Conflict(new { message = $"Scheduling conflict: {conflict.ConflictType} '{conflict.ConflictTitle}'", conflict });
            }

            game.GameDate = request.GameDate;
            game.RinkId = request.RinkId;
            game.HomeTeamId = request.HomeTeamId;
            game.AwayTeamId = request.AwayTeamId;
            game.HomeScore = request.HomeScore;
            game.AwayScore = request.AwayScore;
            game.Location = request.Location;
            if (!string.IsNullOrWhiteSpace(request.Status))
                game.Status = request.Status;

            var updated = await _gameRepository.UpdateAsync(game);
            return Ok(MapToDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating game {GameId}", id);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/admin/games/{id}/cancel
    /// Marks a game as Cancelled (soft cancel — does not delete).
    /// </summary>
    [HttpPut("games/{id}/cancel")]
    public async Task<IActionResult> CancelGame(int id)
    {
        try
        {
            if (!await IsAdminAsync()) return Forbid();

            var game = await _gameRepository.GetByIdAsync(id);
            if (game == null) return NotFound(new { message = "Game not found" });

            if (game.Status == "Cancelled")
                return BadRequest(new { message = "Game is already cancelled" });

            await _gameRepository.CancelAsync(id);
            return Ok(new { message = "Game cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling game {GameId}", id);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    private static GameSummaryDto MapToDto(Game g) => new()
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
    };
}
