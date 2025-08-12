namespace HockeyRinkAPI.Models
{
    public class SessionRegistration
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public int  SessionId { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ApplicationUser? User { get; set; }
        public Session? Session { get; set; }
        public List<Payment> Payments { get; set; } = new();
    }
}
