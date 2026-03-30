namespace HockeyRinkAPI.Models.Responses;

/// <summary>
/// Day view response: all bookings for a rink on a specific date.
/// </summary>
public class DayBookingsResponse
{
    public int RinkId { get; set; }
    public string RinkName { get; set; } = null!;
    public DateTime Date { get; set; }
    public List<CalendarSlotDto> Slots { get; set; } = new();
}
