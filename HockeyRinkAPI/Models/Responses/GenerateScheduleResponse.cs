namespace HockeyRinkAPI.Models.Responses;

public class GenerateScheduleResponse
{
    public List<ProposedGameDto> ProposedGames { get; set; } = new();
    public List<SkippedDateDto> SkippedDates { get; set; } = new();
    public List<UnscheduledMatchupDto> UnscheduledMatchups { get; set; } = new();
    public int TotalGamesGenerated { get; set; }
}

public class ProposedGameDto
{
    public DateTime GameDate { get; set; }
    public int HomeTeamId { get; set; }
    public string HomeTeamName { get; set; } = null!;
    public int AwayTeamId { get; set; }
    public string AwayTeamName { get; set; } = null!;
    public int RinkId { get; set; }
    public bool HasConflict { get; set; }
    public string? ConflictReason { get; set; }
}

public class SkippedDateDto
{
    public DateTime Date { get; set; }
    public string Reason { get; set; } = null!;
}

public class UnscheduledMatchupDto
{
    public int HomeTeamId { get; set; }
    public string HomeTeamName { get; set; } = null!;
    public int AwayTeamId { get; set; }
    public string AwayTeamName { get; set; } = null!;
    public string Reason { get; set; } = null!;
}
