using HockeyRinkAPI.Models;

namespace HockeyRinkAPI.Services;

public class SessionActivationService : ISessionActivationService
{
    private readonly ILogger<SessionActivationService> _logger;

    public SessionActivationService(ILogger<SessionActivationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<bool> ApplyActivationRulesAsync(IEnumerable<Session> sessions)
    {
        var now = DateTime.UtcNow;
        bool hasChanges = false;

        foreach (var session in sessions)
        {
            _logger.LogDebug(
                "Checking session {SessionId} '{SessionName}': IsActive={IsActive}, RegOpenDate={RegOpenDate}, Now={Now}, LastModified={LastModified}",
                session.Id,
                session.Name,
                session.IsActive,
                session.RegistrationOpenDate,
                now,
                session.LastModified
            );

            // Auto-activate if registration open date has passed and session is inactive
            if (session.RegistrationOpenDate.HasValue &&
                session.RegistrationOpenDate.Value <= now &&
                !session.IsActive)
            {
                // Auto-activate unless the admin manually deactivated it AFTER the registration opened
                bool manuallyDeactivatedAfterOpen = session.LastModified.HasValue &&
                                                   session.LastModified.Value > session.RegistrationOpenDate.Value;

                _logger.LogInformation(
                    "Session {SessionId} eligible for auto-activation. ManuallyDeactivatedAfterOpen={ManuallyDeactivated}",
                    session.Id,
                    manuallyDeactivatedAfterOpen
                );

                if (!manuallyDeactivatedAfterOpen)
                {
                    session.IsActive = true;
                    hasChanges = true;
                    _logger.LogInformation(
                        "Auto-activated session: {SessionName} (ID: {SessionId}) - Registration opened at: {OpenDate}, Current time: {Now}",
                        session.Name,
                        session.Id,
                        session.RegistrationOpenDate,
                        now
                    );
                }
            }

            // Auto-deactivate sessions where dates have passed
            bool shouldAutoDeactivate = false;
            DateTime? criticalDate = null;

            // Check registration close date
            if (session.RegistrationCloseDate.HasValue && session.RegistrationCloseDate.Value < now)
            {
                criticalDate = session.RegistrationCloseDate.Value;
                shouldAutoDeactivate = true;
            }
            // Check session end date
            else if (session.EndDate < now)
            {
                criticalDate = session.EndDate;
                shouldAutoDeactivate = true;
            }

            // Only deactivate if session is active and either:
            // - Never been manually modified, OR
            // - Last modified before the critical date passed
            if (shouldAutoDeactivate && session.IsActive && criticalDate.HasValue)
            {
                if (!session.LastModified.HasValue || session.LastModified.Value < criticalDate.Value)
                {
                    session.IsActive = false;
                    hasChanges = true;
                    _logger.LogInformation(
                        "Auto-deactivated session: {SessionName} (ID: {SessionId}) - Critical date: {CriticalDate}",
                        session.Name,
                        session.Id,
                        criticalDate
                    );
                }
            }
        }

        return Task.FromResult(hasChanges);
    }
}
