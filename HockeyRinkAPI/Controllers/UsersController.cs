using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HockeyRinkAPI.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        // Check for token-based auth first
        var authHeader = Request.Headers.Authorization.FirstOrDefault();

        ApplicationUser user;
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length);
            var isValidToken = await ValidateTokenAsync(token);
            if (!isValidToken)
            {
                return Unauthorized(new { Message = "Invalid or expired token" });
            }
            // Extract user from token
            var tokenData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = tokenData.Split('|');
            var userId = parts[0];
            user = await _userManager.FindByIdAsync(userId)!;
        }
        else
        {
            // Fall back to cookie auth
            user = await _userManager.GetUserAsync(User)!;
        }

        if (user == null)
            return NotFound(new { Message = "User not found" });

        return Ok(
            new
            {
                user.Email,
                user.FirstName,
                user.LastName,
                user.LeagueId,
            }
        );
    }

    private async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = tokenData.Split('|');

            if (parts.Length != 3)
                return false;

            var userId = parts[0];
            var email = parts[1];
            var expiry = DateTime.Parse(parts[2]);

            if (expiry < DateTime.UtcNow)
                return false;

            var user = await _userManager.FindByIdAsync(userId);
            return user != null && user.Email == email;
        }
        catch
        {
            return false;
        }
    }
}
