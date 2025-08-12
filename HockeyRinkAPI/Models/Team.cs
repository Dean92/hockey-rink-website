namespace HockeyRinkAPI.Models
{
    public class Team
    {
        public int Id { get; set; }
        public int LeagueId { get; set; }
        public string? Name { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public League? League { get; set; }
        public List<Player>? Players { get; set; }
    }
}
