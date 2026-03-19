namespace HockeyRinkAPI.Models.Requests;

public class UpdateLeagueModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public decimal? EarlyBirdPrice { get; set; }
    public DateTime? EarlyBirdEndDate { get; set; }
    public decimal? RegularPrice { get; set; }
    public DateTime? RegistrationOpenDate { get; set; }
    public DateTime? RegistrationCloseDate { get; set; }
}
