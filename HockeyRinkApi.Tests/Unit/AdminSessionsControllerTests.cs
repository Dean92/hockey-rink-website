using HockeyRinkAPI.Controllers.Admin;
using HockeyRinkAPI.Models;
using HockeyRinkAPI.Models.Requests;
using HockeyRinkAPI.Repositories;
using HockeyRinkAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HockeyRinkApi.Tests.Unit;

public class AdminSessionsControllerTests
{
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<ISessionRepository> _mockSessionRepo;
    private readonly Mock<ISessionActivationService> _mockActivationService;
    private readonly AdminSessionsController _controller;

    public AdminSessionsControllerTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockTokenService = new Mock<ITokenService>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);
        _mockSessionRepo = new Mock<ISessionRepository>();
        _mockActivationService = new Mock<ISessionActivationService>();

        _controller = new AdminSessionsController(
            _mockTokenService.Object,
            _mockUserManager.Object,
            _mockSessionRepo.Object,
            _mockActivationService.Object,
            NullLogger<AdminSessionsController>.Instance);

        SetupAdminContext();
    }

    private void SetupAdminContext()
    {
        const string token = "test-token";
        const string adminId = "admin-id";
        var adminUser = new ApplicationUser { Id = adminId, Email = "admin@test.com" };
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers.Authorization = $"Bearer {token}";
        _controller.ControllerContext = new ControllerContext { HttpContext = ctx };
        _mockTokenService.Setup(t => t.GetUserIdFromTokenAsync(token)).ReturnsAsync(adminId);
        _mockUserManager.Setup(m => m.FindByIdAsync(adminId)).ReturnsAsync(adminUser);
        _mockUserManager.Setup(m => m.IsInRoleAsync(adminUser, "Admin")).ReturnsAsync(true);
    }

    [Fact]
    public async Task GetAllSessions_ReturnsOk_WithSessionList()
    {
        var sessions = new List<Session>
        {
            new Session { Id = 1, Name = "Session A", EndDate = DateTime.UtcNow.AddDays(7) }
        };
        _mockSessionRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(sessions);
        _mockActivationService
            .Setup(s => s.ApplyActivationRulesAsync(It.IsAny<IEnumerable<Session>>()))
            .ReturnsAsync(false);

        var result = await _controller.GetAllSessions();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAllSessions_SavesChanges_WhenActivationHasChanges()
    {
        var sessions = new List<Session>
        {
            new Session { Id = 1, Name = "Session A", EndDate = DateTime.UtcNow.AddDays(7) }
        };
        _mockSessionRepo.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(sessions);
        _mockActivationService
            .Setup(s => s.ApplyActivationRulesAsync(It.IsAny<IEnumerable<Session>>()))
            .ReturnsAsync(true);
        _mockSessionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _controller.GetAllSessions();

        _mockSessionRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSession_ReturnsNotFound_WhenSessionDoesNotExist()
    {
        _mockSessionRepo.Setup(r => r.GetByIdWithLeagueAsync(99)).ReturnsAsync((Session?)null);

        var result = await _controller.GetSession(99);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetSession_ReturnsOk_WhenSessionExists()
    {
        var session = new Session { Id = 1, Name = "Test", EndDate = DateTime.UtcNow.AddDays(7) };
        _mockSessionRepo.Setup(r => r.GetByIdWithLeagueAsync(1)).ReturnsAsync(session);

        var result = await _controller.GetSession(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CreateSession_ReturnsOk_WithValidModel()
    {
        var model = new CreateSessionModel
        {
            Name = "New Session",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(8),
            Fee = 50m,
            MaxPlayers = 20
        };
        _mockSessionRepo.Setup(r => r.AddAsync(It.IsAny<Session>())).Returns(Task.CompletedTask);
        _mockSessionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _controller.CreateSession(model);

        Assert.IsType<OkObjectResult>(result);
        _mockSessionRepo.Verify(r => r.AddAsync(It.IsAny<Session>()), Times.Once);
        _mockSessionRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateSession_ReturnsNotFound_WhenSessionMissing()
    {
        _mockSessionRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Session?)null);
        var model = new UpdateSessionModel { Name = "Updated", EndDate = DateTime.UtcNow.AddDays(7) };

        var result = await _controller.UpdateSession(99, model);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateSession_ReturnsOk_WhenSessionExists()
    {
        var session = new Session { Id = 1, Name = "Old", EndDate = DateTime.UtcNow.AddDays(7) };
        _mockSessionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(session);
        _mockSessionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var model = new UpdateSessionModel { Name = "Updated", EndDate = DateTime.UtcNow.AddDays(7) };

        var result = await _controller.UpdateSession(1, model);

        Assert.IsType<OkObjectResult>(result);
        _mockSessionRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteSession_ReturnsNotFound_WhenSessionDoesNotExist()
    {
        _mockSessionRepo.Setup(r => r.GetByIdWithRegistrationsAsync(99)).ReturnsAsync((Session?)null);

        var result = await _controller.DeleteSession(99);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteSession_DeactivatesSession_WhenHasRegistrations()
    {
        var session = new Session
        {
            Id = 1,
            Name = "Test",
            IsActive = true,
            EndDate = DateTime.UtcNow.AddDays(7),
            SessionRegistrations = new List<SessionRegistration> { new SessionRegistration() }
        };
        _mockSessionRepo.Setup(r => r.GetByIdWithRegistrationsAsync(1)).ReturnsAsync(session);
        _mockSessionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _controller.DeleteSession(1);

        Assert.IsType<OkObjectResult>(result);
        Assert.False(session.IsActive);
        _mockSessionRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteSession_RemovesSession_WhenNoRegistrations()
    {
        var session = new Session
        {
            Id = 1,
            Name = "Test",
            EndDate = DateTime.UtcNow.AddDays(7),
            SessionRegistrations = new List<SessionRegistration>()
        };
        _mockSessionRepo.Setup(r => r.GetByIdWithRegistrationsAsync(1)).ReturnsAsync(session);
        _mockSessionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _controller.DeleteSession(1);

        Assert.IsType<OkObjectResult>(result);
        _mockSessionRepo.Verify(r => r.Remove(session), Times.Once);
    }

    [Fact]
    public async Task PublishDraft_ReturnsBadRequest_WhenDraftNotEnabled()
    {
        var session = new Session { Id = 1, Name = "Test", DraftEnabled = false, EndDate = DateTime.UtcNow.AddDays(7) };
        _mockSessionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(session);
        var model = new PublishDraftModel { Published = true };

        var result = await _controller.PublishDraft(1, model);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task PublishDraft_PublishesDraft_WhenDraftEnabled()
    {
        var session = new Session { Id = 1, Name = "Test", DraftEnabled = true, EndDate = DateTime.UtcNow.AddDays(7) };
        _mockSessionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(session);
        _mockSessionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var model = new PublishDraftModel { Published = true };

        var result = await _controller.PublishDraft(1, model);

        Assert.IsType<OkObjectResult>(result);
        Assert.True(session.DraftPublished);
    }
}
