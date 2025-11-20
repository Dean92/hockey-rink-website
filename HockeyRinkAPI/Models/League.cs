namespace HockeyRinkAPI.Models
{
    public class League
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartDate { get; set; }
        public decimal? EarlyBirdPrice { get; set; }
        public DateTime? EarlyBirdEndDate { get; set; }
        public decimal? RegularPrice { get; set; }
        public DateTime? RegistrationOpenDate { get; set; }
        public DateTime? RegistrationCloseDate { get; set; }
        public List<Team>? Teams { get; set; }
    }
}
