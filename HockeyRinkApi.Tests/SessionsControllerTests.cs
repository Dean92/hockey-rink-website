using HockeyRinkAPI;
using Microsoft.AspNetCore.Mvc.Testing;
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

        // Act
        var response = await client.PostAsJsonAsync("/api/sessions/register", new { sessionId = 1 });

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



