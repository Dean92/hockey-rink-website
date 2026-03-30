namespace HockeyRinkAPI.Models.Requests;

public class BlockoutRequest
{
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? Reason { get; set; }
}
