namespace HockeyRinkAPI.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? Type { get; set; }
        public string? Content { get; set; }
        public DateTime SentAt { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ApplicationUser? User { get; set; }
    }
}
