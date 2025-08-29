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

    public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
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
            LeagueId = null
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            _logger.LogWarning("User registration failed for {Email}: {Errors}", model.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        _logger.LogInformation("Mock email confirmation for {Email}: Token={Token}", user.Email, token);

        return Ok(new { Message = "User registered. Check email for confirmation.", Token = token });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
        {
            _logger.LogWarning("Invalid login attempt: missing email or password");
            return BadRequest(new { Message = "Email and Password are required" });
        }

        var users = await _userManager.Users
            .Where(u => u.Email == model.Email)
            .ToListAsync();

        if (users.Count > 1)
        {
            _logger.LogWarning("Multiple users found with email {Email}. Login aborted.", model.Email);
            return StatusCode(500, new { Message = "Internal server error: duplicate users" });
        }

        var user = users.SingleOrDefault();
        if (user == null || !user.EmailConfirmed)
        {
            _logger.LogWarning("Login failed: user not found or unconfirmed for {Email}", model.Email);
            return Unauthorized(new { Message = "Invalid email or unconfirmed account" });
        }

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            _logger.LogInformation("Login successful for {Email}", model.Email);
            return Ok(new { Message = "Login successful" });
        }

        _logger.LogWarning("Login failed: invalid credentials for {Email}", model.Email);
        return Unauthorized(new { Message = "Invalid credentials" });
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
            _logger.LogWarning("Email confirmation failed for {Email}: {Errors}", model.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
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