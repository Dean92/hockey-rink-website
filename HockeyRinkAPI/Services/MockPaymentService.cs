namespace HockeyRinkAPI.Services
{
    public class MockPaymentService : IPaymentService
    {
        private readonly ILogger<MockPaymentService> _logger;

        public MockPaymentService(ILogger<MockPaymentService> logger)
        {
            _logger = logger;
        }

        public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request)
        {
            _logger.LogInformation(
                "Processing mock payment: Amount=${Amount}, Card ending in {LastFour}",
                request.Amount,
                request.CardNumber.Length >= 4 ? request.CardNumber[^4..] : "****"
            );

            // Simulate processing delay (1-2 seconds)
            var delay = Random.Shared.Next(1000, 2000);
            await Task.Delay(delay);

            var response = new PaymentResponse
            {
                ProcessedAt = DateTime.UtcNow
            };

            // Remove spaces from card number for comparison
            var cardNumber = request.CardNumber.Replace(" ", "");

            // Test card scenarios
            if (cardNumber == "4242424242424242")
            {
                // Success scenario
                response.Success = true;
                response.TransactionId = $"MOCK_TXN_{Guid.NewGuid()}";
                _logger.LogInformation(
                    "Mock payment successful: TransactionId={TransactionId}",
                    response.TransactionId
                );
            }
            else if (cardNumber == "4000000000000002")
            {
                // Decline scenario
                response.Success = false;
                response.ErrorMessage = "Your card was declined. Please use a different payment method.";
                _logger.LogWarning("Mock payment declined: Card {CardNumber}", cardNumber);
            }
            else if (cardNumber.Length == 16 && cardNumber.All(char.IsDigit))
            {
                // Any other valid 16-digit card number = success
                response.Success = true;
                response.TransactionId = $"MOCK_TXN_{Guid.NewGuid()}";
                _logger.LogInformation(
                    "Mock payment successful: TransactionId={TransactionId}",
                    response.TransactionId
                );
            }
            else
            {
                // Invalid card number
                response.Success = false;
                response.ErrorMessage = "Invalid card number. Please check and try again.";
                _logger.LogWarning("Mock payment failed: Invalid card number format");
            }

            return response;
        }
    }
}
