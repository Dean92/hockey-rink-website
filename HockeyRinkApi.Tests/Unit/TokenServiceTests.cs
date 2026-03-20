using HockeyRinkAPI.Models;
using HockeyRinkAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HockeyRinkApi.Tests.Unit;

public class TokenServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly TokenService _sut;

    public TokenServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);
        _sut = new TokenService(_mockUserManager.Object, NullLogger<TokenService>.Instance);
    }

    [Fact]
    public void GenerateToken_ReturnsValidBase64()
    {
        var user = new ApplicationUser { Id = "user1", Email = "test@test.com" };

        var token = _sut.GenerateToken(user);
        var bytes = Convert.FromBase64String(token);

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task ValidateTokenAsync_ValidToken_ReturnsTrue()
    {
        var user = new ApplicationUser { Id = "user1", Email = "test@test.com" };
        var token = _sut.GenerateToken(user);
        _mockUserManager.Setup(m => m.FindByIdAsync("user1")).ReturnsAsync(user);

        var result = await _sut.ValidateTokenAsync(token);

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_ExpiredToken_ReturnsFalse()
    {
        var expiry = DateTime.UtcNow.AddHours(-1);
        var tokenData = $"user1|test@test.com|{expiry:O}";
        var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(tokenData));

        var result = await _sut.ValidateTokenAsync(token);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_InvalidBase64_ReturnsFalse()
    {
        var result = await _sut.ValidateTokenAsync("not-valid-base64!!!");

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_WrongEmail_ReturnsFalse()
    {
        var user = new ApplicationUser { Id = "user1", Email = "test@test.com" };
        var expiry = DateTime.UtcNow.AddHours(24);
        var tokenData = $"user1|wrong@test.com|{expiry:O}";
        var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(tokenData));
        _mockUserManager.Setup(m => m.FindByIdAsync("user1")).ReturnsAsync(user);

        var result = await _sut.ValidateTokenAsync(token);

        Assert.False(result);
    }

    [Fact]
    public async Task GetUserIdFromTokenAsync_ValidToken_ReturnsUserId()
    {
        var user = new ApplicationUser { Id = "user1", Email = "test@test.com" };
        var token = _sut.GenerateToken(user);
        _mockUserManager.Setup(m => m.FindByIdAsync("user1")).ReturnsAsync(user);

        var result = await _sut.GetUserIdFromTokenAsync(token);

        Assert.Equal("user1", result);
    }

    [Fact]
    public async Task GetUserIdFromTokenAsync_ExpiredToken_ReturnsNull()
    {
        var expiry = DateTime.UtcNow.AddHours(-1);
        var tokenData = $"user1|test@test.com|{expiry:O}";
        var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(tokenData));

        var result = await _sut.GetUserIdFromTokenAsync(token);

        Assert.Null(result);
    }
}
