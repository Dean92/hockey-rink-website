using HockeyRinkAPI.Models;
using HockeyRinkAPI.Repositories;
using HockeyRinkAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HockeyRinkAPI.Controllers.Admin;

[Route("api/admin")]
public class AdminDashboardController : AdminControllerBase
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly ILogger<AdminDashboardController> _logger;

    public AdminDashboardController(
        ITokenService tokenService,
        UserManager<ApplicationUser> userManager,
        ISessionRepository sessionRepository,
        IRegistrationRepository registrationRepository,
        ILogger<AdminDashboardController> logger)
        : base(tokenService, userManager)
    {
        _sessionRepository = sessionRepository;
        _registrationRepository = registrationRepository;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardAnalytics()
    {
        try
        {
            if (!await IsAdminOrSubAdminAsync())
                return Forbid();

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var todaysRegistrations = await _registrationRepository.GetByDateRangeAsync(today, tomorrow);

            var now = DateTime.UtcNow;
            var activeSessionEntities = await _sessionRepository.GetActiveSessionsWithDetailsAsync(now);
            var activeSessions = activeSessionEntities
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    LeagueName = s.League != null ? s.League.Name : null,
                    s.StartDate,
                    s.EndDate,
                    s.MaxPlayers,
                    RegisteredCount = s.SessionRegistrations.Count,
                    SpotsRemaining = s.MaxPlayers - s.SessionRegistrations.Count,
                    TotalRevenue = s.SessionRegistrations.Sum(r => r.AmountPaid),
                    s.RegularPrice
                })
                .ToList();

            var totalRevenue = await _registrationRepository.GetTotalRevenueAsync();

            var currentYear = DateTime.UtcNow.Year;
            var yearRevenue = await _registrationRepository.GetYearRevenueAsync(currentYear);

            var thisMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var monthRevenue = await _registrationRepository.GetRevenueFromDateAsync(thisMonthStart);
            var todayRevenue = await _registrationRepository.GetRevenueFromDateAsync(today);

            var nextWeek = DateTime.UtcNow.AddDays(7);
            var upcomingSessionEntities = await _sessionRepository.GetUpcomingAsync(DateTime.UtcNow, nextWeek);
            var upcomingSessions = upcomingSessionEntities
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    LeagueName = s.League != null ? s.League.Name : null,
                    s.StartDate,
                    s.EndDate,
                    RegisteredCount = s.SessionRegistrations.Count,
                    s.MaxPlayers
                })
                .ToList();

            return Ok(new
            {
                todaysRegistrationsCount = todaysRegistrations.Count,
                activeSessionsCount = activeSessions.Count,
                totalRevenue,
                yearRevenue,
                revenueYear = currentYear,
                monthRevenue,
                todayRevenue,
                activeSessions,
                upcomingSessions,
                recentRegistrations = todaysRegistrations
                    .OrderByDescending(r => r.RegistrationDate)
                    .Take(10)
                    .Select(r => new
                    {
                        r.Id,
                        r.Name,
                        r.Email,
                        r.SessionId,
                        r.RegistrationDate,
                        r.AmountPaid
                    })
                    .ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard analytics");
            return StatusCode(500, new { error = "Internal Server Error", details = ex.Message });
        }
    }
}
