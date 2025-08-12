namespace HockeyRinkAPI.Models
{
    public class Player
    {
        public string UserId { get; set; }
        public int TeamId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public ApplicationUser? User { get; set; }
        public Team? Team { get; set; }
    }
}
