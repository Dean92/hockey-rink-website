using HockeyRinkAPI.Models;
using HockeyRinkAPI.Models.Requests;
using HockeyRinkAPI.Repositories;
using HockeyRinkAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HockeyRinkAPI.Controllers.Admin;

[Route("api/admin")]
public class AdminRinksController : AdminControllerBase
{
    private readonly IRinkRepository _rinkRepository;
    private readonly ILogger<AdminRinksController> _logger;

    public AdminRinksController(
        ITokenService tokenService,
        UserManager<ApplicationUser> userManager,
        IRinkRepository rinkRepository,
        ILogger<AdminRinksController> logger)
        : base(tokenService, userManager)
    {
        _rinkRepository = rinkRepository;
        _logger = logger;
    }

    [HttpGet("rinks")]
    public async Task<IActionResult> GetAllRinks()
    {
        try
        {
            if (!await IsAdminAsync()) return Forbid();

            var rinks = await _rinkRepository.GetAllAsync();
            return Ok(rinks.Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                r.IsActive,
                r.CreatedAt
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching rinks");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPost("rinks")]
    public async Task<IActionResult> CreateRink([FromBody] RinkRequest model)
    {
        try
        {
            if (!await IsAdminAsync()) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var rink = new Rink
            {
                Name = model.Name,
                Description = model.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _rinkRepository.AddAsync(rink);
            await _rinkRepository.SaveChangesAsync();

            _logger.LogInformation("Rink created: {RinkName} (ID: {RinkId})", rink.Name, rink.Id);
            return Ok(new { message = $"Rink \"{rink.Name}\" created successfully", id = rink.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rink");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPut("rinks/{id}")]
    public async Task<IActionResult> UpdateRink(int id, [FromBody] RinkRequest model)
    {
        try
        {
            if (!await IsAdminAsync()) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var rink = await _rinkRepository.GetByIdAsync(id);
            if (rink == null) return NotFound(new { message = "Rink not found" });

            rink.Name = model.Name;
            rink.Description = model.Description;

            await _rinkRepository.SaveChangesAsync();

            _logger.LogInformation("Rink updated: {RinkName} (ID: {RinkId})", rink.Name, rink.Id);
            return Ok(new { message = $"Rink \"{rink.Name}\" updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rink");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpDelete("rinks/{id}")]
    public async Task<IActionResult> DeactivateRink(int id)
    {
        try
        {
            if (!await IsAdminAsync()) return Forbid();

            var rink = await _rinkRepository.GetByIdAsync(id);
            if (rink == null) return NotFound(new { message = "Rink not found" });

            await _rinkRepository.DeactivateAsync(rink);
            await _rinkRepository.SaveChangesAsync();

            _logger.LogInformation("Rink deactivated: {RinkName} (ID: {RinkId})", rink.Name, id);
            return Ok(new { message = $"Rink \"{rink.Name}\" deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating rink");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }
}

