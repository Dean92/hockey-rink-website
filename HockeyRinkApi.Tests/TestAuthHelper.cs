using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Linq;


namespace HockeyRinkApi.Tests;

public class TestAuthHelper
{
    private readonly HttpClient _client;
    private readonly IServiceProvider _serviceProvider;
    private string? _currentToken;

    public TestAuthHelper(HttpClient client, IServiceProvider serviceProvider)
    {
        _client = client;
        _serviceProvider = serviceProvider;
    }

    public async Task LoginAsync(string email, string password)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TestAuthHelper>>();

        // Don't call EnsureDeletedAsync or EnsureCreatedAsync for InMemory database
        // The database is already created by the test factory

        // Ensure Leisure league exists
        var leisureLeague = await db.Leagues.FirstOrDefaultAsync(l => l.Name == "Leisure");
        if (leisureLeague == null)
        {
            logger.LogInformation("Seeding Leisure league");
            leisureLeague = new League { Name = "Leisure", Description = "Beginner league" };
            db.Leagues.Add(leisureLeague);
            await db.SaveChangesAsync();
        }

        // Seed sessions
        if (!db.Sessions.Any())
        {
            logger.LogInformation("Seeding sessions");
            db.Sessions.Add(new Session
            {
                Name = "Test Session",
                StartDate = DateTime.Parse("2025-09-01"),
                EndDate = DateTime.Parse("2025-12-01"),
                Fee = 100.00m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Check if user already exists
        var existingUser = await userManager.FindByEmailAsync(email);

        if (existingUser == null)
        {
            // Create and confirm new user
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                NormalizedUserName = email.ToUpperInvariant(),
                FirstName = "Test",
                LastName = "User",
                IsSubAvailable = false,
                LeagueId = leisureLeague.Id,
                EmailConfirmed = false
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                logger.LogWarning("User creation failed: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmResult = await userManager.ConfirmEmailAsync(user, token);
            if (!confirmResult.Succeeded)
            {
                logger.LogWarning("Email confirmation failed for new user {Email}: {Errors}", email, string.Join(", ", confirmResult.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("User {Email} created and confirmed", email);
        }
        else
        {
            logger.LogInformation("User {Email} already exists, skipping creation", email);
        }

        // Attempt login
        var loginModel = new
        {
            Email = email,
            Password = password,
            RememberMe = true
        };

        logger.LogInformation("Attempting login for {Email}", email);
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginModel);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            logger.LogError("Login failed: StatusCode={StatusCode}, Content={Content}", response.StatusCode, content);
            throw new HttpRequestException($"Login failed: {response.StatusCode} ({content})");
        }

        // Extract token from response
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (loginResponse?.Token != null)
        {
            _currentToken = loginResponse.Token;
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _currentToken);
            logger.LogInformation("Login successful for {Email}, IsAdmin: {IsAdmin}", email, loginResponse.IsAdmin);
        }
        else
        {
            logger.LogWarning("Login succeeded but no token received for {Email}", email);
        }
    }

    private class LoginResponse
    {
        public string? Token { get; set; }
        public string? Message { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public bool IsAdmin { get; set; }
    }
}



