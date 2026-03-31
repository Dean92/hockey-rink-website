using HockeyRinkAPI.Models;

namespace HockeyRinkAPI.Services;

public class RegistrationPricingService : IRegistrationPricingService
{
    private readonly ILogger<RegistrationPricingService> _logger;

    public RegistrationPricingService(ILogger<RegistrationPricingService> logger)
    {
        _logger = logger;
    }

    public decimal CalculatePrice(Session session, string? position, DateTime registrationDate)
    {
        // Goalies get a flat goalie price when one is configured — overrides all other tiers
        if (position == PositionConstants.Goalie && session.GoaliePrice.HasValue)
        {
            _logger.LogInformation("Applying goalie price {GoaliePrice} for session {SessionId}",
                session.GoaliePrice.Value, session.Id);
            return session.GoaliePrice.Value;
        }

        // Early bird window
        if (session.EarlyBirdPrice.HasValue &&
            session.EarlyBirdEndDate.HasValue &&
            registrationDate <= session.EarlyBirdEndDate.Value)
        {
            _logger.LogInformation("Applying early bird price {EarlyBirdPrice} for session {SessionId}",
                session.EarlyBirdPrice.Value, session.Id);
            return session.EarlyBirdPrice.Value;
        }

        var regularPrice = session.RegularPrice ?? session.Fee;
        _logger.LogInformation("Applying regular price {RegularPrice} for session {SessionId}",
            regularPrice, session.Id);
        return regularPrice;
    }
}
