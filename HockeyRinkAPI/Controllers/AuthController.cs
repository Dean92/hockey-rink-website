using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using HockeyRinkAPI.Models;
using HockeyRinkAPI.Services;

namespace HockeyRinkAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AuthController> _logger;
    private readonly ITokenService _tokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AuthController> logger,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        try
        {
            _logger.LogInformation("Starting registration for email: {Email}", model?.Email);
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for registration");
                return BadRequest(ModelState);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Position = model.Position,
                EmergencyContactName = model.EmergencyContactName,
                EmergencyContactPhone = model.EmergencyContactPhone,
                Phone = model.Phone,
                Address = model.Address,
                City = model.City,
                State = model.State,
                ZipCode = model.ZipCode,
                DateOfBirth = model.DateOfBirth,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    _logger.LogWarning("User creation failed: {Error}", error.Description);
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(ModelState);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            var token = _tokenService.GenerateToken(user);

            _logger.LogInformation("User registered and logged in successfully: {Email}", model.Email);
            return Ok(new
            {
                message = "User registered successfully",
                token = token
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register user {Email}", model?.Email);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", model?.Email);
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for login");
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                _logger.LogWarning("User not found: {Email}", model.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Invalid password for user: {Email}", model.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await _signInManager.SignInAsync(user, isPersistent: false);
            var token = _tokenService.GenerateToken(user);

            // Check if user is admin
            var roles = await _userManager.GetRolesAsync(user);
            var isAdmin = roles.Contains("Admin");

            _logger.LogInformation("User logged in successfully: {Email}, IsAdmin: {IsAdmin}", model.Email, isAdmin);

            return Ok(new
            {
                token = token,
                message = "Login successful",
                userId = user.Id,
                email = user.Email,
                isAdmin = isAdmin
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to login user {Email}", model?.Email);
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            await _signInManager.SignOutAsync();
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to logout user");
            return StatusCode(500, new { error = "Internal Server Error" });
        }
    }

    [HttpGet("validate")]
    public async Task<IActionResult> ValidateToken()
    {
        try
        {
            _logger.LogInformation("ValidateToken called");

            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            _logger.LogInformation("Authorization header: {AuthHeader}", authHeader);

            // Check token-based auth first
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                _logger.LogInformation("Validating token: {Token}", token);
                var isValid = await _tokenService.ValidateTokenAsync(token);
                _logger.LogInformation("Token validation result: {IsValid}", isValid);
                return Ok(new { isValid });
            }

            // Fall back to cookie auth
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Cookie authenticated, userId: {UserId}", userId);
                return Ok(new { isValid = true });
            }

            _logger.LogWarning("No valid authentication found");
            return Ok(new { isValid = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }

    // Validate password setup token
    [HttpGet("setup-password/{token}")]
    public async Task<IActionResult> ValidatePasswordSetupToken(string token)
    {
        try
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PasswordSetupToken == token);

            if (user == null)
            {
                return NotFound(new { message = "Invalid or expired token" });
            }

            if (!user.PasswordSetupTokenExpiry.HasValue ||
                user.PasswordSetupTokenExpiry.Value < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Token has expired" });
            }

            return Ok(new
            {
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password setup token");
            return StatusCode(500, new { error = "Internal Server Error" });
        }
    }

    // Setup password for manually registered user
    [HttpPost("setup-password")]
    public async Task<IActionResult> SetupPassword([FromBody] SetupPasswordModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PasswordSetupToken == model.Token);

            if (user == null)
            {
                return NotFound(new { message = "Invalid or expired token" });
            }

            if (!user.PasswordSetupTokenExpiry.HasValue ||
                user.PasswordSetupTokenExpiry.Value < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Token has expired" });
            }

            // Set the password
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(ModelState);
            }

            // Clear setup token and mark as not manually registered anymore
            user.PasswordSetupToken = null;
            user.PasswordSetupTokenExpiry = null;
            user.IsManuallyRegistered = false;
            user.EmailConfirmed = true;

            await _userManager.UpdateAsync(user);

            // Sign the user in
            await _signInManager.SignInAsync(user, isPersistent: false);
            var authToken = _tokenService.GenerateToken(user);

            _logger.LogInformation("User {Email} completed password setup", user.Email);

            return Ok(new
            {
                message = "Password setup successful",
                token = authToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up password");
            return StatusCode(500, new { error = "Internal Server Error" });
        }
    }
}

public class RegisterModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Position { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
}

public class LoginModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}