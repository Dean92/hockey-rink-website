using HockeyRinkAPI.Models;
using HockeyRinkAPI.Models.Requests;
using HockeyRinkAPI.Models.Responses;
using HockeyRinkAPI.Repositories;
using HockeyRinkAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HockeyRinkAPI.Controllers.Admin;

[Route("api/admin/rink-calendar")]
public class AdminRinkCalendarController : AdminControllerBase
{
    private readonly IRinkCalendarRepository _calendarRepository;
    private readonly IRinkRepository _rinkRepository;
    private readonly IConflictDetectionService _conflictService;
    private readonly ILogger<AdminRinkCalendarController> _logger;

    public AdminRinkCalendarController(
        ITokenService tokenService,
        UserManager<ApplicationUser> userManager,
        IRinkCalendarRepository calendarRepository,
        IRinkRepository rinkRepository,
        IConflictDetectionService conflictService,
        ILogger<AdminRinkCalendarController> logger)
        : base(tokenService, userManager)
    {
        _calendarRepository = calendarRepository;
        _rinkRepository = rinkRepository;
        _conflictService = conflictService;
        _logger = logger;
    }

    /// <summary>GET /api/admin/rink-calendar?rinkId=1&amp;date=2026-04-07</summary>
    [HttpGet]
    public async Task<IActionResult> GetDayBookings([FromQuery] int rinkId, [FromQuery] DateTime date)
    {
        try
        {
            if (!await IsAdminAsync()) return Forbid();

            var rink = await _rinkRepository.GetByIdAsync(rinkId);
            if (rink == null) return NotFound(new { message = "Rink not found" });

            var sessions = await _calendarRepository.GetSessionsForRinkOnDateAsync(rinkId, date);
            var games = await _calendarRepository.GetGamesForRinkOnDateAsync(rinkId, date);
            var blockouts = await _calendarRepository.GetBlockoutsForRinkOnDateAsync(rinkId, date);

            var slots = new List<CalendarSlotDto>();

            slots.AddRange(sessions.Select(s => new CalendarSlotDto
            {
                Id = s.Id,
                Type = "session",
                Title = s.Name,
                StartDateTime = s.StartDate,
                EndDateTime = s.EndDate,
                LeagueName = s.League?.Name,
                Status = s.IsActive ? "active" : "inactive",
                RinkId = rinkId,
                RinkName = rink.Name
            }));

            slots.AddRange(games.Select(g => new CalendarSlotDto
            {
                Id = g.Id,
                Type = "game",
                Title = $"{g.HomeTeam?.TeamName ?? "TBD"} vs {g.AwayTeam?.TeamName ?? "TBD"}",
                StartDateTime = g.GameDate,
                EndDateTime = g.GameDate.AddMinutes(90),    // default 90-min game
                LeagueName = g.Session?.League?.Name,
                HomeTeamName = g.HomeTeam?.TeamName,
                AwayTeamName = g.AwayTeam?.TeamName,
                Status = g.Status,
                RinkId = rinkId,
                RinkName = rink.Name
            }));

            slots.AddRange(blockouts.Select(b => new CalendarSlotDto
            {
                Id = b.Id,
                Type = "blockout",
                Title = b.Reason ?? "Rink Blockout",
                StartDateTime = b.StartDateTime,
                EndDateTime = b.EndDateTime,
                Reason = b.Reason,
                RinkId = rinkId,
                RinkName = rink.Name
            }));

            var response = new DayBookingsResponse
            {
                RinkId = rinkId,
                RinkName = rink.Name,
                Date = date.Date,
                Slots = slots.OrderBy(s => s.StartDateTime).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching day bookings for rink {RinkId} on {Date}", rinkId, date);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    /// <summary>GET /api/admin/rink-calendar/month?rinkId=1&amp;year=2026&amp;month=4</summary>
    [HttpGet("month")]
    public async Task<IActionResult> GetMonthBookingCounts([FromQuery] int rinkId, [FromQuery] int year, [FromQuery] int month)
    {
        try
        {
            if (!await IsAdminAsync()) return Forbid();

            var rink = await _rinkRepository.GetByIdAsync(rinkId);
            if (rink == null) return NotFound(new { message = "Rink not found" });

            var counts = await _calendarRepository.GetMonthBookingCountsAsync(rinkId, year, month);

            var response = new MonthBookingCountsResponse
            {
                RinkId = rinkId,
                Year = year,
                Month = month,
                DayCounts = counts
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching month booking counts for rink {RinkId} {Year}/{Month}", rinkId, year, month);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    /// <summary>POST /api/admin/rink-calendar/check-conflict</summary>
    [HttpPost("check-conflict")]
    public async Task<IActionResult> CheckConflict([FromBody] ConflictCheckRequest request)
    {
        try
        {
            if (!await IsAdminAsync()) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (request.EndDateTime <= request.StartDateTime)
                return BadRequest(new { message = "End time must be after start time" });

            var result = await _conflictService.CheckAsync(
                request.RinkId,
                request.StartDateTime,
                request.EndDateTime,
                request.ExcludeGameId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking conflict for rink {RinkId}", request.RinkId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }
}
