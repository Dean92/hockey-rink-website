using System;
using System.Collections.Generic;

namespace HockeyRinkAPI.Models;

public partial class Session
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime StartDate { get; set; }

    public TimeSpan? StartTime { get; set; }

    public DateTime EndDate { get; set; }

    public TimeSpan? EndTime { get; set; }

    public decimal Fee { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? LeagueId { get; set; }

    public int? RinkId { get; set; }

    public DateTime? EarlyBirdEndDate { get; set; }

    public decimal? EarlyBirdPrice { get; set; }

    public int MaxPlayers { get; set; }

    public DateTime? RegistrationCloseDate { get; set; }

    public DateTime? RegistrationOpenDate { get; set; }

    public decimal? RegularPrice { get; set; }

    public DateTime? LastModified { get; set; }

    public bool DraftEnabled { get; set; }

    public bool DraftPublished { get; set; }

    /// <summary>Target total games per team for the regular season.</summary>
    public int? RegularSeasonGames { get; set; }

    /// <summary>Flat price for goalies (overrides regular/early-bird when set and player position is Goalie).</summary>
    public decimal? GoaliePrice { get; set; }

    public virtual League? League { get; set; }

    public virtual Rink? Rink { get; set; }

    public virtual ICollection<SessionRegistration> SessionRegistrations{ get; set; } = new List<SessionRegistration>();

    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
}
