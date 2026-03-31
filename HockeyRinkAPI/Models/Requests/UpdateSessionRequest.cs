namespace HockeyRinkAPI.Models.Requests;

public class UpdateSessionModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Fee { get; set; }
    public bool IsActive { get; set; }
    public bool DraftEnabled { get; set; }
    public int? LeagueId { get; set; }
    public int MaxPlayers { get; set; } = 20;
    public DateTime? RegistrationOpenDate { get; set; }
    public DateTime? RegistrationCloseDate { get; set; }
    public decimal? EarlyBirdPrice { get; set; }
    public DateTime? EarlyBirdEndDate { get; set; }
    public decimal? RegularPrice { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int? RegularSeasonGames { get; set; }
    public decimal? GoaliePrice { get; set; }
}
