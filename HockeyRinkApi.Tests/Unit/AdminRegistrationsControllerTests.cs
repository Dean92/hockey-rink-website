using HockeyRinkAPI.Controllers.Admin;
using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using HockeyRinkAPI.Models.Requests;
using HockeyRinkAPI.Repositories;
using HockeyRinkAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HockeyRinkApi.Tests.Unit;

public class AdminRegistrationsControllerTests : IDisposable
{
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<ISessionRepository> _mockSessionRepo;
    private readonly Mock<IRegistrationRepository> _mockRegistrationRepo;
    private readonly Mock<IPaymentRepository> _mockPaymentRepo;
    private readonly AppDbContext _dbContext;
    private readonly AdminRegistrationsController _controller;

    public AdminRegistrationsControllerTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockTokenService = new Mock<ITokenService>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);
        _mockSessionRepo = new Mock<ISessionRepository>();
        _mockRegistrationRepo = new Mock<IRegistrationRepository>();
        _mockPaymentRepo = new Mock<IPaymentRepository>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);

        _controller = new AdminRegistrationsController(
            _mockTokenService.Object,
            _mockUserManager.Object,
            _mockSessionRepo.Object,
            _mockRegistrationRepo.Object,
            _mockPaymentRepo.Object,
            _dbContext,
            NullLogger<AdminRegistrationsController>.Instance);

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

    public void Dispose() => _dbContext.Dispose();

    [Fact]
    public async Task GetAllRegistrations_ReturnsOk()
    {
        _mockRegistrationRepo
            .Setup(r => r.GetAllWithDetailsAsync())
            .ReturnsAsync(new List<SessionRegistration>());

        var result = await _controller.GetAllRegistrations();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetSessionRegistrations_ReturnsNotFound_WhenSessionMissing()
    {
        _mockSessionRepo.Setup(r => r.GetByIdWithRegistrationsAsync(99)).ReturnsAsync((Session?)null);

        var result = await _controller.GetSessionRegistrations(99);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetSessionRegistrations_ReturnsOk_WithRegistrations()
    {
        var session = new Session
        {
            Id = 1,
            Name = "Test",
            EndDate = DateTime.UtcNow.AddDays(7),
            SessionRegistrations = new List<SessionRegistration>()
        };
        _mockSessionRepo.Setup(r => r.GetByIdWithRegistrationsAsync(1)).ReturnsAsync(session);

        var result = await _controller.GetSessionRegistrations(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AddManualRegistration_ReturnsNotFound_WhenSessionMissing()
    {
        _mockSessionRepo.Setup(r => r.GetByIdWithRegistrationsAsync(99)).ReturnsAsync((Session?)null);
        var model = new ManualRegistrationModel
        {
            Name = "John Doe",
            Email = "john@test.com",
            DateOfBirth = new DateTime(1990, 1, 1),
            AmountPaid = 50m
        };

        var result = await _controller.AddManualRegistration(99, model);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AddManualRegistration_CreatesRegistrationAndPayment_WhenSessionExists()
    {
        var session = new Session
        {
            Id = 1,
            Name = "Test",
            MaxPlayers = 20,
            EndDate = DateTime.UtcNow.AddDays(7),
            SessionRegistrations = new List<SessionRegistration>()
        };
        _mockSessionRepo.Setup(r => r.GetByIdWithRegistrationsAsync(1)).ReturnsAsync(session);
        _mockUserManager.Setup(m => m.FindByEmailAsync("new@test.com")).ReturnsAsync((ApplicationUser?)null);
        _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        _mockRegistrationRepo.Setup(r => r.AddAsync(It.IsAny<SessionRegistration>())).Returns(Task.CompletedTask);
        _mockRegistrationRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mockPaymentRepo.Setup(r => r.AddAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);
        _mockPaymentRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var model = new ManualRegistrationModel
        {
            Name = "New Player",
            Email = "new@test.com",
            DateOfBirth = new DateTime(1990, 1, 1),
            AmountPaid = 50m
        };

        var result = await _controller.AddManualRegistration(1, model);

        Assert.IsType<OkObjectResult>(result);
        _mockRegistrationRepo.Verify(r => r.AddAsync(It.IsAny<SessionRegistration>()), Times.Once);
        _mockPaymentRepo.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Once);
    }

    [Fact]
    public async Task RemoveRegistration_ReturnsNotFound_WhenRegistrationMissing()
    {
        _mockRegistrationRepo
            .Setup(r => r.GetByIdAndSessionAsync(99, 1))
            .ReturnsAsync((SessionRegistration?)null);

        var result = await _controller.RemoveRegistration(1, 99);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task RemoveRegistration_RemovesPaymentAndRegistration()
    {
        var registration = new SessionRegistration
        {
            Id = 1,
            SessionId = 1,
            Name = "Test Player",
            Email = "test@test.com"
        };
        var payment = new Payment { Id = 1, SessionRegistrationId = 1 };
        _mockRegistrationRepo.Setup(r => r.GetByIdAndSessionAsync(1, 1)).ReturnsAsync(registration);
        _mockPaymentRepo.Setup(r => r.GetByRegistrationIdAsync(1)).ReturnsAsync(payment);
        _mockRegistrationRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _controller.RemoveRegistration(1, 1);

        Assert.IsType<OkObjectResult>(result);
        _mockPaymentRepo.Verify(r => r.Remove(payment), Times.Once);
        _mockRegistrationRepo.Verify(r => r.Remove(registration), Times.Once);
    }
}
