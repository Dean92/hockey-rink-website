using System;

namespace HockeyRinkAPI.Models;

public partial class Game
{
    public int Id { get; set; }

    public DateTime GameDate { get; set; }

    public int SessionId { get; set; }

    public int HomeTeamId { get; set; }

    public int AwayTeamId { get; set; }

    public int? HomeScore { get; set; }

    public int? AwayScore { get; set; }

    public string Status { get; set; } = "Scheduled"; // Scheduled, InProgress, Completed, Cancelled

    public string? Location { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Session Session { get; set; } = null!;

    public virtual Team HomeTeam { get; set; } = null!;

    public virtual Team AwayTeam { get; set; } = null!;
}
