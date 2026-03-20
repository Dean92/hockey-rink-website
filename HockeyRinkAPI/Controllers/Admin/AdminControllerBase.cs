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

    protected async Task<bool> IsAdminAsync()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length);
            var userId = await _tokenService.GetUserIdFromTokenAsync(token);
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                    return await _userManager.IsInRoleAsync(user, "Admin");
            }
        }
        else if (HttpContext.User.Identity?.IsAuthenticated == true)
        {
            var userId = HttpContext
                .User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                    return await _userManager.IsInRoleAsync(user, "Admin");
            }
        }

        return false;
    }
}
