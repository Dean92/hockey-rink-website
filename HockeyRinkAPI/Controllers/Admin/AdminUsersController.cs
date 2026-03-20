using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using HockeyRinkAPI.Models.Requests;
using HockeyRinkAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HockeyRinkAPI.Controllers.Admin;

[Route("api/admin")]
public class AdminUsersController : AdminControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        ITokenService tokenService,
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext,
        ILogger<AdminUsersController> logger)
        : base(tokenService, userManager)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            if (!await IsAdminAsync())
                return Forbid();

            var users = await _dbContext
                .Users.Include(u => u.League)
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.LeagueId,
                    LeagueName = u.League != null ? u.League.Name :
                        _dbContext.SessionRegistrations
                            .Where(sr => sr.UserId == u.Id)
                            .OrderByDescending(sr => sr.RegistrationDate)
                            .Select(sr => sr.Session.League.Name)
                            .FirstOrDefault(),
                    u.EmailConfirmed,
                    u.CreatedAt,
                    u.Rating,
                    u.PlayerNotes,
                    u.Position,
                    u.Address,
                    u.City,
                    u.State,
                    u.ZipCode,
                    u.Phone,
                    u.DateOfBirth,
                    u.LastLoginAt,
                    u.EmergencyContactName,
                    u.EmergencyContactPhone,
                    u.HockeyRegistrationNumber,
                    u.HockeyRegistrationType
                })
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPut("users/{userId}/rating")]
    public async Task<IActionResult> UpdatePlayerRating(string userId, [FromBody] UpdatePlayerRatingModel model)
    {
        try
        {
            if (!await IsAdminAsync())
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            user.Rating = model.Rating;
            user.PlayerNotes = model.PlayerNotes;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { message = "Failed to update user", errors = result.Errors });

            return Ok(new
            {
                message = "Player rating and notes updated successfully",
                userId = user.Id,
                rating = user.Rating,
                playerNotes = user.PlayerNotes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating player rating for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPut("users/{userId}/profile")]
    public async Task<IActionResult> UpdateUserProfile(string userId, [FromBody] UpdateUserProfileModel model)
    {
        try
        {
            if (!await IsAdminAsync())
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Address = model.Address;
            user.City = model.City;
            user.State = model.State;
            user.ZipCode = model.ZipCode;
            user.Phone = model.Phone;
            user.DateOfBirth = model.DateOfBirth;
            user.Position = model.Position;
            user.Rating = model.Rating;
            user.PlayerNotes = model.PlayerNotes;
            user.LeagueId = model.LeagueId;
            user.UpdatedAt = DateTime.UtcNow;

            if (user.Email != model.Email)
            {
                var emailExists = await _userManager.FindByEmailAsync(model.Email);
                if (emailExists != null && emailExists.Id != userId)
                    return BadRequest(new { message = "Email address is already in use" });

                var emailToken = await _userManager.GenerateChangeEmailTokenAsync(user, model.Email);
                var emailResult = await _userManager.ChangeEmailAsync(user, model.Email, emailToken);
                if (!emailResult.Succeeded)
                    return BadRequest(new { message = "Failed to update email", errors = emailResult.Errors });

                user.UserName = model.Email;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { message = "Failed to update user", errors = result.Errors });

            return Ok(new { message = "User profile updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }
}
