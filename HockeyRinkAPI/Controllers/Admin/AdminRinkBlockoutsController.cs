using HockeyRinkAPI.Models;
using HockeyRinkAPI.Models.Requests;
using HockeyRinkAPI.Repositories;
using HockeyRinkAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HockeyRinkAPI.Controllers.Admin;

[Route("api/admin")]
public class AdminRinkBlockoutsController : AdminControllerBase
{
    private readonly IRinkRepository _rinkRepository;
    private readonly ILogger<AdminRinkBlockoutsController> _logger;

    public AdminRinkBlockoutsController(
        ITokenService tokenService,
        UserManager<ApplicationUser> userManager,
        IRinkRepository rinkRepository,
        ILogger<AdminRinkBlockoutsController> logger)
        : base(tokenService, userManager)
    {
        _rinkRepository = rinkRepository;
        _logger = logger;
    }

    [HttpGet("rinks/{rinkId}/blockouts")]
    public async Task<IActionResult> GetBlockouts(int rinkId)
    {
        try
        {
            if (!await HasPermissionAsync(AdminPermissions.ManageSchedule)) return Forbid();

            var blockouts = await _rinkRepository.GetBlockoutsAsync(rinkId);
            return Ok(blockouts.Select(b => new
            {
                b.Id,
                b.RinkId,
                b.StartDateTime,
                b.EndDateTime,
                b.Reason,
                b.EventType,
                b.CreatedAt
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching blockouts for rink {RinkId}", rinkId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPost("rinks/{rinkId}/blockouts")]
    public async Task<IActionResult> CreateBlockout(int rinkId, [FromBody] BlockoutRequest model)
    {
        try
        {
            if (!await HasPermissionAsync(AdminPermissions.ManageSchedule)) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (model.EndDateTime <= model.StartDateTime)
                return BadRequest(new { message = "End time must be after start time" });

            var rink = await _rinkRepository.GetByIdAsync(rinkId);
            if (rink == null) return NotFound(new { message = "Rink not found" });

            var blockout = new RinkBlockout
            {
                RinkId = rinkId,
                StartDateTime = model.StartDateTime,
                EndDateTime = model.EndDateTime,
                Reason = model.Reason,
                EventType = model.EventType,
                CreatedAt = DateTime.UtcNow
            };

            await _rinkRepository.AddBlockoutAsync(blockout);
            await _rinkRepository.SaveChangesAsync();

            _logger.LogInformation("Blockout created for rink {RinkId}: {Start} - {End}", rinkId, blockout.StartDateTime, blockout.EndDateTime);
            return Ok(new { message = "Blockout added successfully", id = blockout.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating blockout for rink {RinkId}", rinkId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPut("rinks/{rinkId}/blockouts/{id}")]
    public async Task<IActionResult> UpdateBlockout(int rinkId, int id, [FromBody] BlockoutRequest model)
    {
        try
        {
            if (!await HasPermissionAsync(AdminPermissions.ManageSchedule)) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (model.EndDateTime <= model.StartDateTime)
                return BadRequest(new { message = "End time must be after start time" });

            var blockout = await _rinkRepository.GetBlockoutByIdAsync(id);
            if (blockout == null || blockout.RinkId != rinkId)
                return NotFound(new { message = "Blockout not found" });

            blockout.StartDateTime = model.StartDateTime;
            blockout.EndDateTime = model.EndDateTime;
            blockout.Reason = model.Reason;
            blockout.EventType = model.EventType;

            await _rinkRepository.SaveChangesAsync();

            _logger.LogInformation("Blockout {BlockoutId} updated for rink {RinkId}", id, rinkId);
            return Ok(new { message = "Blockout updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating blockout {BlockoutId}", id);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpDelete("rinks/{rinkId}/blockouts/{id}")]
    public async Task<IActionResult> DeleteBlockout(int rinkId, int id)
    {
        try
        {
            if (!await HasPermissionAsync(AdminPermissions.ManageSchedule)) return Forbid();

            var blockout = await _rinkRepository.GetBlockoutByIdAsync(id);
            if (blockout == null || blockout.RinkId != rinkId)
                return NotFound(new { message = "Blockout not found" });

            await _rinkRepository.DeleteBlockoutAsync(blockout);
            await _rinkRepository.SaveChangesAsync();

            _logger.LogInformation("Blockout {BlockoutId} deleted from rink {RinkId}", id, rinkId);
            return Ok(new { message = "Blockout removed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blockout {BlockoutId}", id);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }
}
