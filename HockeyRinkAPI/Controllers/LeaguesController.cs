using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;

namespace HockeyRinkAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/league")]
    public class LeaguesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<LeaguesController> _logger;

        public LeaguesController(AppDbContext db, ILogger<LeaguesController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetLeagues()
        {
            try
            {
                var leagues = await _db.Leagues.ToListAsync();
                return Ok(leagues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLeagues");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }
    }
}
