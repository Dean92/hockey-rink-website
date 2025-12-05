using HockeyRinkAPI;
using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;


namespace HockeyRinkApi.Tests;

public class SessionsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public SessionsControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSessions_ReturnsSuccess()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var email = $"test_{Guid.NewGuid()}@testing.com";
        var authHelper = new TestAuthHelper(client, _factory.Services);
        await authHelper.LoginAsync(email, "Test123!");

        var response = await client.GetAsync("/api/sessions");

        if (response.StatusCode == HttpStatusCode.Found)
        {
            var location = response.Headers.Location?.ToString() ?? "unknown";
            throw new HttpRequestException($"Unexpected redirect to: {location} — check cookie persistence or [Authorize] setup.");
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RegisterSession_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var email = $"test_{Guid.NewGuid()}@testing.com";
        var authHelper = new TestAuthHelper(client, _factory.Services);
        await authHelper.LoginAsync(email, "Test123!");

        // Create an active session with future dates
        int sessionId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var league = await db.Leagues.FirstOrDefaultAsync();
            var session = new Session
            {
                Name = "Future Session",
                StartDate = DateTime.UtcNow.AddDays(30),
                EndDate = DateTime.UtcNow.AddDays(60),
                Fee = 100.00m,
                IsActive = true,
                MaxPlayers = 20,
                CreatedAt = DateTime.UtcNow,
                LeagueId = league?.Id
            };
            db.Sessions.Add(session);
            await db.SaveChangesAsync();
            sessionId = session.Id;
        }

        // Act
        var registrationData = new
        {
            sessionId = sessionId,
            name = "Test User",
            email = email,
            dateOfBirth = new DateTime(1990, 1, 1)
        };
        var response = await client.PostAsJsonAsync("/api/sessions/register", registrationData);

        // Assert
        if (response.StatusCode == HttpStatusCode.Found)
        {
            var location = response.Headers.Location?.ToString() ?? "unknown";
            throw new HttpRequestException($"Unexpected redirect to: {location} — check cookie persistence or [Authorize] setup.");
        }
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }
}



