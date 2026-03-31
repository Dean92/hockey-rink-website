using HockeyRinkAPI.Models;
using HockeyRinkAPI.Models.Requests;
using HockeyRinkAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HockeyRinkAPI.Controllers.Admin;

[Route("api/admin/user-management")]
public class AdminUserManagementController : AdminControllerBase
{
    private readonly IAdminUserManagementService _userManagementService;
    private readonly ILogger<AdminUserManagementController> _logger;

    public AdminUserManagementController(
        ITokenService tokenService,
        UserManager<ApplicationUser> userManager,
        IAdminUserManagementService userManagementService,
        ILogger<AdminUserManagementController> logger)
        : base(tokenService, userManager)
    {
        _userManagementService = userManagementService;
        _logger = logger;
    }

    /// <summary>GET /api/admin/user-management/admins — lists all full admins and sub-admins with their permissions.</summary>
    [HttpGet("admins")]
    public async Task<IActionResult> GetAdminUsers()
    {
        try
        {
            if (!await IsAdminAsync()) return Forbid();
            var users = await _userManagementService.GetAllUsersWithPermissionsAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching admin users");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    /// <summary>POST /api/admin/user-management/users/{id}/grant-admin — promotes an existing user to sub-admin.</summary>
    [HttpPost("users/{userId}/grant-admin")]
    public async Task<IActionResult> GrantAdmin(string userId, [FromBody] GrantAdminRequest request)
    {
        try
        {
            if (!await IsAdminAsync()) return Forbid();
            var result = await _userManagementService.GrantAdminAsync(userId, request.Permissions);
            if (!result.Succeeded)
                return BadRequest(new { message = result.ErrorMessage });
            return Ok(new { message = "Admin access granted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting admin to user {UserId}", userId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    /// <summary>PUT /api/admin/user-management/users/{id}/permissions — updates a sub-admin's permissions.</summary>
    [HttpPut("users/{userId}/permissions")]
    public async Task<IActionResult> UpdatePermissions(string userId, [FromBody] UpdatePermissionsRequest request)
    {
        try
        {
            if (!await IsAdminAsync()) return Forbid();
            var result = await _userManagementService.UpdatePermissionsAsync(userId, request.Permissions);
            if (!result.Succeeded)
                return BadRequest(new { message = result.ErrorMessage });
            return Ok(new { message = "Permissions updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permissions for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    /// <summary>DELETE /api/admin/user-management/users/{id}/revoke-admin — removes sub-admin role and all permissions.</summary>
    [HttpDelete("users/{userId}/revoke-admin")]
    public async Task<IActionResult> RevokeAdmin(string userId)
    {
        try
        {
            if (!await IsAdminAsync()) return Forbid();
            var result = await _userManagementService.RevokeAdminAsync(userId);
            if (!result.Succeeded)
                return BadRequest(new { message = result.ErrorMessage });
            return Ok(new { message = "Admin access revoked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking admin from user {UserId}", userId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    /// <summary>POST /api/admin/user-management/invite — invites a user by email (creates account if needed) and grants sub-admin.</summary>
    [HttpPost("invite")]
    public async Task<IActionResult> InviteAdmin([FromBody] InviteAdminRequest request)
    {
        try
        {
            if (!await IsAdminAsync()) return Forbid();
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { message = "Email is required" });

            var result = await _userManagementService.InviteUserAsync(request.Email, request.Permissions);
            if (!result.Succeeded)
                return BadRequest(new { message = result.ErrorMessage });
            return Ok(new { message = $"Admin invitation sent to {request.Email}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inviting admin user {Email}", request.Email);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    /// <summary>GET /api/admin/user-management/all-users — returns all regular users for the promote search.</summary>
    [HttpGet("all-users")]
    public async Task<IActionResult> GetAllUsersForSearch([FromQuery] string? search)
    {
        try
        {
            if (!await IsAdminAsync()) return Forbid();
            var users = await _userManagementService.SearchUsersAsync(search);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }
}
