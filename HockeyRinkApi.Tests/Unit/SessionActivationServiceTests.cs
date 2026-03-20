using HockeyRinkAPI.Models;
using HockeyRinkAPI.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HockeyRinkApi.Tests.Unit;

public class SessionActivationServiceTests
{
    private readonly SessionActivationService _sut;

    public SessionActivationServiceTests()
    {
        _sut = new SessionActivationService(NullLogger<SessionActivationService>.Instance);
    }

    [Fact]
    public async Task ApplyActivationRulesAsync_AutoActivates_WhenRegOpenDatePassed()
    {
        var session = new Session
        {
            Id = 1,
            Name = "Test",
            IsActive = false,
            RegistrationOpenDate = DateTime.UtcNow.AddHours(-1),
            EndDate = DateTime.UtcNow.AddDays(7)
        };

        var hasChanges = await _sut.ApplyActivationRulesAsync(new[] { session });

        Assert.True(hasChanges);
        Assert.True(session.IsActive);
    }

    [Fact]
    public async Task ApplyActivationRulesAsync_DoesNotActivate_WhenManuallyDeactivatedAfterOpen()
    {
        var session = new Session
        {
            Id = 1,
            Name = "Test",
            IsActive = false,
            RegistrationOpenDate = DateTime.UtcNow.AddHours(-2),
            LastModified = DateTime.UtcNow.AddHours(-1),
            EndDate = DateTime.UtcNow.AddDays(7)
        };

        var hasChanges = await _sut.ApplyActivationRulesAsync(new[] { session });

        Assert.False(hasChanges);
        Assert.False(session.IsActive);
    }

    [Fact]
    public async Task ApplyActivationRulesAsync_AutoDeactivates_WhenRegCloseDatePassed()
    {
        var session = new Session
        {
            Id = 1,
            Name = "Test",
            IsActive = true,
            RegistrationCloseDate = DateTime.UtcNow.AddHours(-1),
            EndDate = DateTime.UtcNow.AddDays(7)
        };

        var hasChanges = await _sut.ApplyActivationRulesAsync(new[] { session });

        Assert.True(hasChanges);
        Assert.False(session.IsActive);
    }

    [Fact]
    public async Task ApplyActivationRulesAsync_AutoDeactivates_WhenEndDatePassed()
    {
        var session = new Session
        {
            Id = 1,
            Name = "Test",
            IsActive = true,
            EndDate = DateTime.UtcNow.AddHours(-1)
        };

        var hasChanges = await _sut.ApplyActivationRulesAsync(new[] { session });

        Assert.True(hasChanges);
        Assert.False(session.IsActive);
    }

    [Fact]
    public async Task ApplyActivationRulesAsync_DoesNotDeactivate_WhenManuallyModifiedAfterCriticalDate()
    {
        var session = new Session
        {
            Id = 1,
            Name = "Test",
            IsActive = true,
            RegistrationCloseDate = DateTime.UtcNow.AddHours(-2),
            LastModified = DateTime.UtcNow.AddHours(-1),
            EndDate = DateTime.UtcNow.AddDays(7)
        };

        var hasChanges = await _sut.ApplyActivationRulesAsync(new[] { session });

        Assert.False(hasChanges);
        Assert.True(session.IsActive);
    }

    [Fact]
    public async Task ApplyActivationRulesAsync_ReturnsFalse_WhenNoChanges()
    {
        var session = new Session
        {
            Id = 1,
            Name = "Test",
            IsActive = true,
            RegistrationOpenDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(7)
        };

        var hasChanges = await _sut.ApplyActivationRulesAsync(new[] { session });

        Assert.False(hasChanges);
        Assert.True(session.IsActive);
    }
}
