namespace HockeyRinkAPI.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int SessionRegistrationId { get; set; }
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public SessionRegistration? Registration { get; set; }
    }
}
