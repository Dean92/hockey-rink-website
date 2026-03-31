using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using HockeyRinkAPI.Models.Requests;
using HockeyRinkAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HockeyRinkAPI.Controllers.Admin;

[Route("api/admin/schedule")]
public class AdminScheduleController : AdminControllerBase
{
    private readonly IScheduleGeneratorService _generatorService;
    private readonly IPlayoffSchedulerService _playoffService;
    private readonly AppDbContext _db;
    private readonly ILogger<AdminScheduleController> _logger;

    public AdminScheduleController(
        ITokenService tokenService,
        UserManager<ApplicationUser> userManager,
        IScheduleGeneratorService generatorService,
        IPlayoffSchedulerService playoffService,
        AppDbContext db,
        ILogger<AdminScheduleController> logger)
        : base(tokenService, userManager)
    {
        _generatorService = generatorService;
        _playoffService = playoffService;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/admin/schedule/generate
    /// Returns a proposed schedule preview — nothing is saved to the database.
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateScheduleRequest request)
    {
        try
        {
            if (!await IsAdminAsync()) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (request.EndDate < request.StartDate)
                return BadRequest(new { message = "End date must be on or after start date" });

            if (request.DaysOfWeek.Count == 0)
                return BadRequest(new { message = "At least one day of week is required" });

            if (request.DailyEndTime <= request.DailyStartTime)
                return BadRequest(new { message = "Daily end time must be after daily start time" });

            var result = await _generatorService.GenerateAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating schedule for league {LeagueId}", request.LeagueId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/schedule/confirm
    /// Saves the admin-approved proposed games to the database.
    /// </summary>
    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmScheduleRequest request)
    {
        try
        {
            if (!await IsAdminAsync()) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (request.Games.Count == 0)
                return BadRequest(new { message = "No games provided to save" });

            var now = DateTime.UtcNow;
            var games = request.Games.Select(g => new Game
            {
                GameDate = g.GameDate,
                SessionId = request.SessionId,
                HomeTeamId = g.HomeTeamId,
                AwayTeamId = g.AwayTeamId,
                RinkId = g.RinkId,
                GameType = string.IsNullOrWhiteSpace(g.GameType) ? "RegularSeason" : g.GameType,
                Status = "Scheduled",
                CreatedAt = now,
                UpdatedAt = now
            }).ToList();

            await _db.Games.AddRangeAsync(games);
            await _db.SaveChangesAsync();

            return Ok(new { message = $"{games.Count} game(s) saved successfully", count = games.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming schedule for session {SessionId}", request.SessionId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/schedule/generate-playoffs
    /// Returns a proposed single-elimination playoff bracket preview — nothing is saved.
    /// Teams are seeded by regular-season win-loss record.
    /// </summary>
    [HttpPost("generate-playoffs")]
    public async Task<IActionResult> GeneratePlayoffs([FromBody] GeneratePlayoffRequest request)
    {
        try
        {
            if (!await IsAdminAsync()) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (request.EndDate < request.StartDate)
                return BadRequest(new { message = "End date must be on or after start date" });

            if (request.DaysOfWeek.Count == 0)
                return BadRequest(new { message = "At least one day of week is required" });

            if (request.DailyEndTime <= request.DailyStartTime)
                return BadRequest(new { message = "Daily end time must be after daily start time" });

            var result = await _playoffService.GenerateAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating playoffs for session {SessionId}", request.SessionId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }
}
