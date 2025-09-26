using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HockeyRinkAPI.Controllers;

[ApiController]
[Route("api/leagues")]
public class LeaguesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public LeaguesController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetLeagues()
    {
        Console.WriteLine("LeaguesController.GetLeagues() - Request received");
        
        // Check for token-based auth first
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        Console.WriteLine($"Authorization header: {authHeader}");
        
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length);
            Console.WriteLine($"Token extracted: {token}");
            var isValidToken = await ValidateTokenAsync(token);
            Console.WriteLine($"Token validation result: {isValidToken}");
            if (!isValidToken)
            {
                Console.WriteLine("Token validation failed - returning Unauthorized");
                return Unauthorized(new { Message = "Invalid or expired token" });
            }
            Console.WriteLine("Token validation successful");
        }
        // If no token, fall back to cookie auth
        else if (!HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            Console.WriteLine("No valid token and not cookie authenticated - returning Unauthorized");
            return Unauthorized(new { Message = "Authentication required" });
        }

        Console.WriteLine("Authentication successful - fetching leagues");
        var leagues = _db.Leagues.ToList();
        Console.WriteLine($"Found {leagues.Count} leagues in database");
        Console.WriteLine($"Returning leagues: {System.Text.Json.JsonSerializer.Serialize(leagues)}");
        return Ok(leagues);
    }

    private async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            Console.WriteLine($"ValidateTokenAsync - Token: {token}");
            var tokenData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
            Console.WriteLine($"ValidateTokenAsync - Decoded token data: {tokenData}");
            var parts = tokenData.Split('|');

            if (parts.Length != 3)
            {
                Console.WriteLine($"ValidateTokenAsync - Invalid parts count: {parts.Length}");
                return false;
            }

            var userId = parts[0];
            var email = parts[1];
            var expiry = DateTime.Parse(parts[2]);

            Console.WriteLine($"ValidateTokenAsync - UserId: {userId}, Email: {email}, Expiry: {expiry}");

            if (expiry < DateTime.UtcNow)
            {
                Console.WriteLine("ValidateTokenAsync - Token expired");
                return false;
            }

            var user = await _userManager.FindByIdAsync(userId);
            var isValid = user != null && user.Email == email;
            Console.WriteLine($"ValidateTokenAsync - User found: {user != null}, Email match: {user?.Email == email}, Result: {isValid}");
            return isValid;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ValidateTokenAsync - Exception: {ex.Message}");
            return false;
        }
    }
}
