namespace HockeyRinkAPI.Models
{
    public class Session
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Fee { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<SessionRegistration> Registrations { get; set; } = new();
    }
}
