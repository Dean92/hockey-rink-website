namespace HockeyRinkAPI.Models.Requests;

/// <summary>
/// Request body for checking if a proposed time slot conflicts with existing bookings.
/// </summary>
public class ConflictCheckRequest
{
    public int RinkId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }

    /// <summary>Optional: exclude a specific game ID (used when editing an existing game).</summary>
    public int? ExcludeGameId { get; set; }
}
