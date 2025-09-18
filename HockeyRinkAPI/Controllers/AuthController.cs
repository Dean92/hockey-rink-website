using HockeyRinkAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        ILogger<AuthController> logger
    )
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (
            model == null
            || string.IsNullOrEmpty(model.Email)
            || string.IsNullOrEmpty(model.Password)
        )
        {
            _logger.LogWarning("Invalid registration attempt: missing email or password");
            return BadRequest(new { Message = "Email and Password are required" });
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            NormalizedEmail = model.Email.ToUpperInvariant(),
            NormalizedUserName = model.Email.ToUpperInvariant(),
            FirstName = model.FirstName,
            LastName = model.LastName,
            IsSubAvailable = false,
            LeagueId = null,
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "User registration failed for {Email}: {Errors}",
                model.Email,
                string.Join(", ", result.Errors.Select(e => e.Description))
            );
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        _logger.LogInformation(
            "Mock email confirmation for {Email}: Token={Token}",
            user.Email,
            token
        );

        return Ok(
            new { Message = "User registered. Check email for confirmation.", Token = token }
        );
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (
            model == null
            || string.IsNullOrEmpty(model.Email)
            || string.IsNullOrEmpty(model.Password)
        )
        {
            _logger.LogWarning("Invalid login attempt: missing email or password");
            return BadRequest(new { Message = "Email and Password are required" });
        }

        var users = await _userManager.Users.Where(u => u.Email == model.Email).ToListAsync();

        if (users.Count > 1)
        {
            _logger.LogWarning(
                "Multiple users found with email {Email}. Login aborted.",
                model.Email
            );
            return StatusCode(500, new { Message = "Internal server error: duplicate users" });
        }

        var user = users.SingleOrDefault();
        if (user == null)
        {
            _logger.LogWarning("Login failed: user not found for {Email}", model.Email);
            return Unauthorized(new { Message = "Invalid email or password" });
        }

        // Skip email confirmation check for development
        // TODO: Re-enable email confirmation for production
        // if (!user.EmailConfirmed)
        // {
        //     _logger.LogWarning("Login failed: unconfirmed account for {Email}", model.Email);
        //     return Unauthorized(new { Message = "Please confirm your email before logging in" });
        // }

        // For development: Check password directly and bypass email confirmation
        var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);

        if (passwordValid)
        {
            _logger.LogInformation("Login successful for {Email}", model.Email);

            // For development: Create a simple token (in production, use proper JWT)
            var token = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    $"{user.Id}|{user.Email}|{DateTime.UtcNow.AddHours(24):O}"
                )
            );

            // Also sign in with cookies for compatibility
            await _signInManager.SignInAsync(user, model.RememberMe);

            return Ok(
                new
                {
                    Message = "Login successful",
                    UserId = user.Id,
                    Token = token,
                    Email = user.Email,
                }
            );
        }

        _logger.LogWarning("Login failed: invalid credentials for {Email}", model.Email);
        return Unauthorized(new { Message = "Invalid credentials" });
    }

    [HttpGet("status")]
    public IActionResult GetAuthStatus()
    {
        var isAuthenticated = HttpContext.User.Identity?.IsAuthenticated ?? false;
        var userId = HttpContext.User.Identity?.Name;

        _logger.LogInformation(
            "Auth status check: IsAuthenticated={IsAuthenticated}, UserId={UserId}",
            isAuthenticated,
            userId
        );

        return Ok(
            new
            {
                IsAuthenticated = isAuthenticated,
                UserId = userId,
                Claims = HttpContext.User.Claims.Select(c => new { c.Type, c.Value }).ToList(),
            }
        );
    }

    [HttpPost("validate-token")]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenModel model)
    {
        _logger.LogInformation(
            "Token validation request received. Token present: {TokenPresent}",
            !string.IsNullOrEmpty(model?.Token)
        );

        if (string.IsNullOrEmpty(model?.Token))
        {
            _logger.LogWarning("Token validation failed: Token is required");
            return BadRequest(new { Message = "Token is required" });
        }

        try
        {
            _logger.LogInformation(
                "Attempting to decode token: {Token}",
                model.Token.Substring(0, Math.Min(20, model.Token.Length)) + "..."
            );

            var tokenData = System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(model.Token)
            );
            _logger.LogInformation("Decoded token data: {TokenData}", tokenData);

            var parts = tokenData.Split('|');
            _logger.LogInformation("Token parts count: {PartsCount}", parts.Length);

            if (parts.Length != 3)
            {
                _logger.LogWarning(
                    "Token validation failed: Invalid token format. Expected 3 parts, got {PartsCount}",
                    parts.Length
                );
                return Unauthorized(new { Message = "Invalid token format" });
            }

            var userId = parts[0];
            var email = parts[1];
            var expiryString = parts[2];

            _logger.LogInformation(
                "Token data - UserId: {UserId}, Email: {Email}, Expiry: {Expiry}",
                userId,
                email,
                expiryString
            );

            if (!DateTime.TryParse(expiryString, out var expiry))
            {
                _logger.LogWarning(
                    "Token validation failed: Invalid expiry date format: {ExpiryString}",
                    expiryString
                );
                return Unauthorized(new { Message = "Invalid token expiry" });
            }

            if (expiry < DateTime.UtcNow)
            {
                _logger.LogWarning(
                    "Token validation failed: Token expired. Expiry: {Expiry}, Current: {Current}",
                    expiry,
                    DateTime.UtcNow
                );
                return Unauthorized(new { Message = "Token expired" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning(
                    "Token validation failed: User not found for ID: {UserId}",
                    userId
                );
                return Unauthorized(new { Message = "Invalid token - user not found" });
            }

            if (user.Email != email)
            {
                _logger.LogWarning(
                    "Token validation failed: Email mismatch. Token email: {TokenEmail}, User email: {UserEmail}",
                    email,
                    user.Email
                );
                return Unauthorized(new { Message = "Invalid token - email mismatch" });
            }

            _logger.LogInformation("Token validation successful for user: {UserId}", userId);
            return Ok(
                new
                {
                    IsValid = true,
                    UserId = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed with exception");
            return Unauthorized(new { Message = "Invalid token" });
        }
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailModel model)
    {
        if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Token))
        {
            _logger.LogWarning("Invalid confirm-email attempt: missing email or token");
            return BadRequest(new { Message = "Email and Token are required" });
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            _logger.LogWarning("Confirm-email failed: user not found for {Email}", model.Email);
            return BadRequest(new { Message = "User not found" });
        }

        var result = await _userManager.ConfirmEmailAsync(user, model.Token);
        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "Email confirmation failed for {Email}: {Errors}",
                model.Email,
                string.Join(", ", result.Errors.Select(e => e.Description))
            );
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("Email confirmed for {Email}", model.Email);
        return Ok(new { Message = "Email confirmed" });
    }
}

public class RegisterModel
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class LoginModel
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public bool RememberMe { get; set; }
}

public class ConfirmEmailModel
{
    public string? Email { get; set; }
    public string? Token { get; set; }
}

public class ValidateTokenModel
{
    public string? Token { get; set; }
}
