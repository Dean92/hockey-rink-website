namespace HockeyRinkAPI.Models.Responses;

/// <summary>
/// Represents a single time slot on the rink calendar (session, game, or blockout).
/// </summary>
public class CalendarSlotDto
{
    public int? Id { get; set; }
    public string Type { get; set; } = null!;       // "session" | "game" | "blockout"
    public string Title { get; set; } = null!;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? LeagueName { get; set; }
    public string? HomeTeamName { get; set; }
    public string? AwayTeamName { get; set; }
    public string? Status { get; set; }
    public string? Reason { get; set; }             // For blockouts
    public int? RinkId { get; set; }
    public string? RinkName { get; set; }
}
