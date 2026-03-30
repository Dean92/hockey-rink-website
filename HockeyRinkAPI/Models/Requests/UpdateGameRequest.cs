namespace HockeyRinkAPI.Models.Requests;

public class UpdateGameRequest
{
    public DateTime GameDate { get; set; }
    public int? RinkId { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public string? Status { get; set; }
    public string? Location { get; set; }
    public int? ExcludeGameId { get; set; }
}
