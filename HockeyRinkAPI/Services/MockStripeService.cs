namespace HockeyRinkAPI.Services
{
    public class MockStripeService
    {
        public Task<string> ProcessPayment(decimal amount)
        {
            return Task.FromResult($"MockTransaction_{Guid.NewGuid()}");
        }
    }
}
