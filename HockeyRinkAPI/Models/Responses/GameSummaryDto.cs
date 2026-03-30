namespace HockeyRinkAPI.Models.Responses;

public class GameSummaryDto
{
    public int Id { get; set; }
    public DateTime GameDate { get; set; }
    public int SessionId { get; set; }
    public string SessionName { get; set; } = null!;
    public int? RinkId { get; set; }
    public string? RinkName { get; set; }
    public int HomeTeamId { get; set; }
    public string HomeTeamName { get; set; } = null!;
    public int AwayTeamId { get; set; }
    public string AwayTeamName { get; set; } = null!;
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public string Status { get; set; } = null!;
    public string? Location { get; set; }
}
