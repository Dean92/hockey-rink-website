namespace HockeyRinkAPI.Models.Responses;

/// <summary>
/// Month view response: booking count per day for badge indicators on the calendar.
/// </summary>
public class MonthBookingCountsResponse
{
    public int RinkId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }

    /// <summary>Key = day of month (1–31), Value = number of bookings.</summary>
    public Dictionary<int, int> DayCounts { get; set; } = new();
}
