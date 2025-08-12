namespace HockeyRinkAPI.Models
{
    public class League
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<Team>? Teams { get; set; }
    }
}
