using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using HockeyRinkAPI;
using System.Net;
using System.Net.Http;

namespace HockeyRinkApi.Tests;

public class LeaguesControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public LeaguesControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetLeagues_ReturnsSuccess()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var email = $"test_{Guid.NewGuid()}@testing.com";
        var authHelper = new TestAuthHelper(client, _factory.Services);
        await authHelper.LoginAsync(email, "Test123!");

        var response = await client.GetAsync("/api/leagues");

        if (response.StatusCode == HttpStatusCode.Found)
        {
            var location = response.Headers.Location?.ToString() ?? "unknown";
            throw new HttpRequestException($"Unexpected redirect to: {location} — check cookie persistence or [Authorize] setup.");
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

