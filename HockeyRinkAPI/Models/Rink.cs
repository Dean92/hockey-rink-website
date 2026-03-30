namespace HockeyRinkAPI.Models;

public class Rink
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<RinkBlockout> Blockouts { get; set; } = new List<RinkBlockout>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual ICollection<Game> Games { get; set; } = new List<Game>();
}
