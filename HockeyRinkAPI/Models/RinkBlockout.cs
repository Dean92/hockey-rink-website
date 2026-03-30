namespace HockeyRinkAPI.Models;

public class RinkBlockout
{
    public int Id { get; set; }

    public int RinkId { get; set; }

    public DateTime StartDateTime { get; set; }

    public DateTime EndDateTime { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Rink Rink { get; set; } = null!;
}
