namespace HockeyRinkAPI.Models;

public class RinkBlockout
{
    public int Id { get; set; }

    public int RinkId { get; set; }

    public DateTime StartDateTime { get; set; }

    public DateTime EndDateTime { get; set; }

    public string? Reason { get; set; }

    /// <summary>Describes the type of event: Blockout, Ice Rental, Private Party, League Game, Youth Game, Public Skating, Other.</summary>
    public string EventType { get; set; } = "Blockout";

    public DateTime CreatedAt { get; set; }

    public virtual Rink Rink { get; set; } = null!;
}
