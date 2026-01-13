using System;
using System.Collections.Generic;

namespace HockeyRinkAPI.Models;

public partial class Team
{
    public int Id { get; set; }

    public string? TeamColor { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CaptainName { get; set; }

    public string? CaptainUserId { get; set; }

    public int SessionId { get; set; }

    public string TeamName { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }

    public int? MaxPlayers { get; set; }

    public virtual ApplicationUser? Captain { get; set; }

    public virtual ICollection<Player> Players { get; set; } = new List<Player>();

    public virtual Session Session { get; set; } = null!;
}
