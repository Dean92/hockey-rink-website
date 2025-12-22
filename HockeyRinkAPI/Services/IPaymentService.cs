namespace HockeyRinkAPI.Services
{
    public interface IPaymentService
    {
        Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request);
    }

    public class PaymentRequest
    {
        public required string CardNumber { get; set; }
        public required string ExpiryDate { get; set; } // Format: MM/YY
        public required string Cvv { get; set; }
        public required string CardholderName { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
    }

    public class PaymentResponse
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}
