using HockeyRinkAPI;
using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace HockeyRinkApi.Tests;

public class AdminRegistrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public AdminRegistrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient client, int sessionId)> SetupAdminTestAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        // Use TestAuthHelper to create and login a regular user first
        var email = $"admin_{Guid.NewGuid()}@testing.com";
        var authHelper = new TestAuthHelper(client, _factory.Services);
        await authHelper.LoginAsync(email, "Test123!");

        // Now upgrade this user to admin
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure Admin role exists
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        // Add user to Admin role
        var user = await userManager.FindByEmailAsync(email);
        if (user != null)
        {
            await userManager.AddToRoleAsync(user, "Admin");
            // Ensure role is committed to database
            await db.SaveChangesAsync();
        }

        // Login again to get Admin role in claims
        await authHelper.LoginAsync(email, "Test123!");

        // Create a test session
        var league = await db.Leagues.FirstOrDefaultAsync();
        if (league == null)
        {
            league = new League
            {
                Name = "Test League",
                Description = "Test league for registration tests",
                CreatedAt = DateTime.UtcNow
            };
            db.Leagues.Add(league);
            await db.SaveChangesAsync();
        }

        var session = new Session
        {
            Name = "Test Session for Registration",
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(60),
            Fee = 150.00m,
            RegularPrice = 150.00m,
            IsActive = true,
            MaxPlayers = 20,
            CreatedAt = DateTime.UtcNow,
            LeagueId = league.Id
        };

        db.Sessions.Add(session);
        await db.SaveChangesAsync();

        return (client, session.Id);
    }

    [Fact]
    public async Task GetSessionRegistrations_ReturnsRegistrationsList()
    {
        // Arrange
        var (client, sessionId) = await SetupAdminTestAsync();

        // Act
        var response = await client.GetAsync($"/api/admin/sessions/{sessionId}/registrations");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task AddManualRegistration_CreatesRegistrationAndPayment()
    {
        // Arrange
        var (client, sessionId) = await SetupAdminTestAsync();

        var registrationData = new
        {
            name = "John Doe",
            email = $"john_{Guid.NewGuid()}@test.com",
            phone = "1234567890",
            address = "123 Main St",
            city = "TestCity",
            state = "TX",
            zipCode = "12345",
            dateOfBirth = "1990-01-01",
            position = "Forward",
            amountPaid = 150.00m
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/admin/sessions/{sessionId}/registrations/manual",
            registrationData
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("success", content.ToLower());

        // Verify registration was created
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var registration = await db.SessionRegistrations
            .Include(r => r.Payments)
            .FirstOrDefaultAsync(r => r.Email == registrationData.email);

        Assert.NotNull(registration);
        Assert.Equal(registrationData.name, registration.Name);
        Assert.Equal(registrationData.phone, registration.Phone);
        Assert.NotNull(registration.Payments);
        Assert.Single(registration.Payments);
        Assert.Equal(registrationData.amountPaid, registration.Payments.First().Amount);
    }

    [Fact]
    public async Task AddManualRegistration_ValidatesRequiredFields()
    {
        // Arrange
        var (client, sessionId) = await SetupAdminTestAsync();

        var invalidData = new
        {
            name = "",  // Missing required field
            email = "invalid-email",  // Invalid email format
            phone = "",
            address = "",
            city = "",
            state = "",
            zipCode = "",
            dateOfBirth = "",
            position = "",
            amountPaid = 0
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/admin/sessions/{sessionId}/registrations/manual",
            invalidData
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRegistration_UpdatesRegistrationAndPayment()
    {
        // Arrange
        var (client, sessionId) = await SetupAdminTestAsync();

        // Create initial registration
        var initialData = new
        {
            name = "Jane Smith",
            email = $"jane_{Guid.NewGuid()}@test.com",
            phone = "9876543210",
            address = "456 Oak Ave",
            city = "TestCity",
            state = "CA",
            zipCode = "54321",
            dateOfBirth = "1985-05-15",
            position = "Defense",
            amountPaid = 150.00m
        };

        await client.PostAsJsonAsync(
            $"/api/admin/sessions/{sessionId}/registrations/manual",
            initialData
        );

        // Get registration ID
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var registration = await db.SessionRegistrations
            .FirstOrDefaultAsync(r => r.Email == initialData.email);
        Assert.NotNull(registration);

        // Updated data
        var updatedData = new
        {
            name = "Jane Smith-Updated",
            email = initialData.email,
            phone = "5555555555",
            address = "789 New St",
            city = "NewCity",
            state = "NY",
            zipCode = "99999",
            dateOfBirth = "1985-05-15",
            position = "Forward",
            amountPaid = 175.00m
        };

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/admin/sessions/{sessionId}/registrations/{registration.Id}",
            updatedData
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify updates
        await db.Entry(registration).ReloadAsync();
        await db.Entry(registration).Collection(r => r.Payments).LoadAsync();

        Assert.Equal("Jane Smith-Updated", registration.Name);
        Assert.Equal("5555555555", registration.Phone);
        Assert.Equal("789 New St", registration.Address);
        Assert.Equal("NewCity", registration.City);
        Assert.Equal("NY", registration.State);
        Assert.Equal("Forward", registration.Position);
        Assert.NotNull(registration.Payments);
        Assert.Single(registration.Payments);
        Assert.Equal(175.00m, registration.Payments.First().Amount);
    }

    [Fact]
    public async Task DeleteRegistration_RemovesRegistrationAndPayment()
    {
        // Arrange
        var (client, sessionId) = await SetupAdminTestAsync();

        // Create registration
        var registrationData = new
        {
            name = "Delete Test",
            email = $"delete_{Guid.NewGuid()}@test.com",
            phone = "1112223333",
            address = "Delete St",
            city = "DeleteCity",
            state = "DL",
            zipCode = "00000",
            dateOfBirth = "1995-01-01",
            position = "Goalie",
            amountPaid = 150.00m
        };

        await client.PostAsJsonAsync(
            $"/api/admin/sessions/{sessionId}/registrations/manual",
            registrationData
        );

        // Get registration ID - use new scope after POST
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var registration = await db.SessionRegistrations
            .Include(r => r.Payments)
            .FirstOrDefaultAsync(r => r.Email == registrationData.email);
        Assert.NotNull(registration);
        var registrationId = registration.Id;
        var paymentId = registration.Payments.FirstOrDefault()?.Id;

        // Act
        var response = await client.DeleteAsync(
            $"/api/admin/sessions/{sessionId}/registrations/{registrationId}"
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify deletion - use new scope after DELETE
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var deletedRegistration = await verifyDb.SessionRegistrations
            .FirstOrDefaultAsync(r => r.Id == registrationId);
        Assert.Null(deletedRegistration);

        // Verify payment is also deleted
        if (paymentId.HasValue)
        {
            var deletedPayment = await verifyDb.Payments
                .FirstOrDefaultAsync(p => p.Id == paymentId.Value);
            Assert.Null(deletedPayment);
        }
    }

    [Fact]
    public async Task GetSessionRegistrations_IncludesCapacityInformation()
    {
        // Arrange
        var (client, sessionId) = await SetupAdminTestAsync();

        // Add a registration
        var registrationData = new
        {
            name = "Capacity Test",
            email = $"capacity_{Guid.NewGuid()}@test.com",
            phone = "4445556666",
            address = "Capacity St",
            city = "CapCity",
            state = "CP",
            zipCode = "11111",
            dateOfBirth = "1992-06-06",
            position = "Center",
            amountPaid = 150.00m
        };

        await client.PostAsJsonAsync(
            $"/api/admin/sessions/{sessionId}/registrations/manual",
            registrationData
        );

        // Act
        var response = await client.GetAsync($"/api/admin/sessions/{sessionId}/registrations");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("maxPlayers", content);
        Assert.Contains("registrations", content);
    }

    [Fact]
    public async Task NonAdminUser_CannotAccessRegistrationManagement()
    {
        // Arrange - Create non-admin user
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var email = $"user_{Guid.NewGuid()}@testing.com";
        var authHelper = new TestAuthHelper(client, _factory.Services);
        await authHelper.LoginAsync(email, "Test123!");

        // Create a session using factory services
        int sessionId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var league = await db.Leagues.FirstOrDefaultAsync();
            if (league == null)
            {
                league = new League
                {
                    Name = "Test League",
                    Description = "Test league",
                    CreatedAt = DateTime.UtcNow
                };
                db.Leagues.Add(league);
                await db.SaveChangesAsync();
            }

            var session = new Session
            {
                Name = "Test Session",
                StartDate = DateTime.UtcNow.AddDays(30),
                EndDate = DateTime.UtcNow.AddDays(60),
                Fee = 150.00m,
                IsActive = true,
                MaxPlayers = 20,
                CreatedAt = DateTime.UtcNow,
                LeagueId = league.Id
            };
            db.Sessions.Add(session);
            await db.SaveChangesAsync();
            sessionId = session.Id;
        }

        // Act
        var response = await client.GetAsync($"/api/admin/sessions/{sessionId}/registrations");

        // Assert
        // Forbid() with cookie auth returns Found (redirect) instead of Forbidden
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Found,
            $"Expected Forbidden or Found, but got {response.StatusCode}"
        );
    }
}
