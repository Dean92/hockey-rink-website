using System;
using System.Collections.Generic;

namespace HockeyRinkAPI.Models;

public partial class Session
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal Fee { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public int LeagueId { get; set; }

    public DateTime? EarlyBirdEndDate { get; set; }

    public decimal? EarlyBirdPrice { get; set; }

    public int MaxPlayers { get; set; }

    public DateTime? RegistrationCloseDate { get; set; }

    public DateTime? RegistrationOpenDate { get; set; }

    public decimal? RegularPrice { get; set; }

    public DateTime? LastModified { get; set; }

    public virtual League League { get; set; } = null!;

    public virtual ICollection<SessionRegistration> SessionRegistrations { get; set; } = new List<SessionRegistration>();

    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
}
