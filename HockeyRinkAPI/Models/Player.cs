using System;
using System.Collections.Generic;

namespace HockeyRinkAPI.Models;

public partial class Player
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public int TeamId { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual Team Team { get; set; } = null!;

    public virtual ApplicationUser User { get; set; } = null!;
}
