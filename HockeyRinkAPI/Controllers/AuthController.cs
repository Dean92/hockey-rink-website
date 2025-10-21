using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using HockeyRinkAPI.Models;

namespace HockeyRinkAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
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
            var token = GenerateToken(user);

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

            await _signInManager.SignInAsync(user, isPersistent: false);
            var token = GenerateToken(user);

            _logger.LogInformation("User logged in successfully: {Email}", model.Email);
            _logger.LogInformation("Generated token for user {Email}: {Token}", model.Email, token);

            return Ok(new
            {
                token = token,
                message = "Login successful",
                userId = user.Id,
                email = user.Email
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
                var isValid = await ValidateTokenAsync(token);
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

    private string GenerateToken(ApplicationUser user)
    {
        var expiry = DateTime.UtcNow.AddHours(24);
        var tokenData = $"{user.Id}|{user.Email}|{expiry:O}";
        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenData);
        return Convert.ToBase64String(tokenBytes);
    }

    private async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            _logger.LogInformation("ValidateTokenAsync - Decoding token: {Token}", token);
            var tokenData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
            _logger.LogInformation("ValidateTokenAsync - Decoded data: {TokenData}", tokenData);

            var parts = tokenData.Split('|');
            if (parts.Length != 3)
            {
                _logger.LogWarning("ValidateTokenAsync - Invalid token format, parts count: {Count}", parts.Length);
                return false;
            }

            var userId = parts[0];
            var email = parts[1];
            var expiry = DateTime.Parse(parts[2]);

            _logger.LogInformation("ValidateTokenAsync - UserId: {UserId}, Email: {Email}, Expiry: {Expiry}",
                userId, email, expiry);

            if (expiry < DateTime.UtcNow)
            {
                _logger.LogWarning("ValidateTokenAsync - Token expired");
                return false;
            }

            var user = await _userManager.FindByIdAsync(userId);
            var isValid = user != null && user.Email == email;
            _logger.LogInformation("ValidateTokenAsync - User found: {UserFound}, Email match: {EmailMatch}, Result: {IsValid}",
                user != null, user?.Email == email, isValid);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ValidateTokenAsync - Exception occurred");
            return false;
        }
    }
}

public class RegisterModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}