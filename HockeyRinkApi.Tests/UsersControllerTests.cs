using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using System.Net;
using System.Net.Http;
using HockeyRinkAPI;

namespace HockeyRinkApi.Tests;

public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public UsersControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProfile_ReturnsSuccess()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var email = $"test_{Guid.NewGuid()}@testing.com";
        var authHelper = new TestAuthHelper(client, _factory.Services);
        await authHelper.LoginAsync(email, "Test123!");

        var response = await client.GetAsync("/api/users/profile");

        if (response.StatusCode == HttpStatusCode.Found)
        {
            var location = response.Headers.Location?.ToString() ?? "unknown";
            throw new HttpRequestException($"Unexpected redirect to: {location} — check cookie persistence or [Authorize] setup.");
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}




