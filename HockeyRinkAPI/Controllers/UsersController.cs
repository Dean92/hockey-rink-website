using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;

namespace HockeyRinkAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<UsersController> _logger;

        public UsersController(AppDbContext db, ILogger<UsersController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var user = await _db.Users
                    .Include(u => u.League)
                    .FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {Email}", User.Identity?.Name);
                    return NotFound(new { Message = "User not found" });
                }
                return Ok(new
                {
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    League = user.League?.Name
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProfile");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }
    }
}
