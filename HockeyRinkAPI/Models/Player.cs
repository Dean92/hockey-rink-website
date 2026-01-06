using System;
using System.Collections.Generic;

namespace HockeyRinkAPI.Models;

public partial class Player
{
    public int Id { get; set; }

    public string? UserId { get; set; }

    public int TeamId { get; set; }

    public int SessionRegistrationId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Team Team { get; set; } = null!;

    public virtual ApplicationUser? User { get; set; }

    public virtual SessionRegistration SessionRegistration { get; set; } = null!;
}
