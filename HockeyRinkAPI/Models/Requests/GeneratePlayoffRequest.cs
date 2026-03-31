using System.ComponentModel.DataAnnotations;

namespace HockeyRinkAPI.Models.Requests;

public class GeneratePlayoffRequest
{
    [Required]
    public int SessionId { get; set; }

    [Required]
    public int RinkId { get; set; }

    /// <summary>First date playoffs may be scheduled on.</summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>Last date playoffs may be scheduled on.</summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>Days of week to schedule games on. 0=Sun … 6=Sat.</summary>
    [Required]
    public List<int> DaysOfWeek { get; set; } = new();

    /// <summary>Earliest game start time on any given day (e.g. "18:00").</summary>
    [Required]
    public TimeSpan DailyStartTime { get; set; }

    /// <summary>Latest time a game may START on any given day.</summary>
    [Required]
    public TimeSpan DailyEndTime { get; set; }

    /// <summary>Duration of each game in minutes. Default 90.</summary>
    [Range(30, 240)]
    public int GameLengthMinutes { get; set; } = 90;

    /// <summary>Buffer between consecutive games on the same rink in minutes. Default 10.</summary>
    [Range(0, 60)]
    public int BufferMinutes { get; set; } = 10;

    /// <summary>Maximum games per night on this rink.</summary>
    [Range(1, 20)]
    public int GamesPerNight { get; set; } = 2;

    /// <summary>Exclude standard US holidays when true.</summary>
    public bool ExcludeUsHolidays { get; set; } = true;

    /// <summary>Additional specific dates to exclude (ISO date strings).</summary>
    public List<string> ExcludeDates { get; set; } = new();
}
