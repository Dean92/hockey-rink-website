using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HockeyRinkAPI.Controllers;

[ApiController]
[Route("api/leagues")]
public class LeaguesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LeaguesController> _logger;

    public LeaguesController(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger<LeaguesController> logger
    )
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous] // Allow public access to view leagues
    public async Task<IActionResult> GetLeagues()
    {
        try
        {
            _logger.LogInformation("GetLeagues - Request received (public endpoint)");

            var leagues = await _db.Leagues.ToListAsync();
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
