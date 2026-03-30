namespace HockeyRinkAPI.Models.Responses;

/// <summary>
/// Result of a conflict check for a proposed rink time slot.
/// </summary>
public class ConflictCheckResponse
{
    public bool HasConflict { get; set; }
    public string? ConflictType { get; set; }       // "session" | "game" | "blockout"
    public string? ConflictTitle { get; set; }
    public DateTime? ConflictStart { get; set; }
    public DateTime? ConflictEnd { get; set; }
}
