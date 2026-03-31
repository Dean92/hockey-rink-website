using HockeyRinkAPI.Models;

namespace HockeyRinkAPI.Services;

/// <summary>
/// Calculates the registration price for a session based on position and pricing tiers.
/// Follows SRP: pricing logic lives here, not in controllers.
/// </summary>
public interface IRegistrationPricingService
{
    /// <summary>
    /// Returns the amount to charge for a registration given the session configuration,
    /// the registrant's position, and the registration date.
    /// Priority: Goalie flat rate → Early bird → Regular price.
    /// </summary>
    decimal CalculatePrice(Session session, string? position, DateTime registrationDate);
}
