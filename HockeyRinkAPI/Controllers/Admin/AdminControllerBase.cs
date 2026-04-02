using HockeyRinkAPI.Models;
using HockeyRinkAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HockeyRinkAPI.Controllers.Admin;

[ApiController]
public abstract class AdminControllerBase : ControllerBase
{
    protected readonly ITokenService _tokenService;
    protected readonly UserManager<ApplicationUser> _userManager;

    protected AdminControllerBase(
        ITokenService tokenService,
        UserManager<ApplicationUser> userManager)
    {
        _tokenService = tokenService;
        _userManager = userManager;
    }

    protected async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length);
            var userId = await _tokenService.GetUserIdFromTokenAsync(token);
            if (!string.IsNullOrEmpty(userId))
                return await _userManager.FindByIdAsync(userId);
        }
        else if (HttpContext.User.Identity?.IsAuthenticated == true)
        {
            var userId = HttpContext
                .User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?.Value;
            if (!string.IsNullOrEmpty(userId))
                return await _userManager.FindByIdAsync(userId);
        }
        return null;
    }

    /// <summary>Returns true only for full Admins.</summary>
    protected async Task<bool> IsAdminAsync()
    {
        var user = await GetCurrentUserAsync();
        return user != null && await _userManager.IsInRoleAsync(user, "Admin");
    }

    /// <summary>Returns true for full Admins or SubAdmins (any admin role).</summary>
    protected async Task<bool> IsAdminOrSubAdminAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return false;
        return await _userManager.IsInRoleAsync(user, "Admin")
            || await _userManager.IsInRoleAsync(user, "SubAdmin");
    }

    /// <summary>
    /// Returns true if the user is a full Admin, or a SubAdmin with the specified permission claim.
    /// </summary>
    protected async Task<bool> HasPermissionAsync(string permission)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return false;
        if (await _userManager.IsInRoleAsync(user, "Admin")) return true;
        if (!await _userManager.IsInRoleAsync(user, "SubAdmin")) return false;
        var claims = await _userManager.GetClaimsAsync(user);
        return claims.Any(c => c.Type == AdminPermissions.ClaimType && c.Value == permission);
    }
}
