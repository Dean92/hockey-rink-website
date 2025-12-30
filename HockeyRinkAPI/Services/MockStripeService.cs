namespace HockeyRinkAPI.Services
{
    public class MockStripeService
    {
        public async Task<PaymentResult> ProcessPayment(decimal amount, string cardNumber, string expiryDate, string cvv, string cardholderName)
        {
            // Simulate processing time
            await Task.Delay(1500);

            // Remove spaces from card number
            var cleanCardNumber = cardNumber.Replace(" ", "");

            // Test card numbers
            // 4242424242424242 - Always succeeds
            // 4000000000000002 - Always fails (card declined)
            if (cleanCardNumber == "4000000000000002")
            {
                return new PaymentResult
                {
                    Success = false,
                    TransactionId = null,
                    ErrorMessage = "Card declined. Your card was declined. Please use a different card.",
                    ProcessedAt = DateTime.UtcNow
                };
            }

            // All other valid card numbers succeed
            return new PaymentResult
            {
                Success = true,
                TransactionId = $"MOCK_TXN_{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                ErrorMessage = null,
                ProcessedAt = DateTime.UtcNow
            };
        }

        // Legacy method for backward compatibility
        public Task<string> ProcessPayment(decimal amount)
        {
            return Task.FromResult($"MockTransaction_{Guid.NewGuid()}");
        }
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}
