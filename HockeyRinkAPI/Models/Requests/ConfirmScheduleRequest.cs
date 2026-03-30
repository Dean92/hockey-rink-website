using System.ComponentModel.DataAnnotations;

namespace HockeyRinkAPI.Models.Requests;

public class ConfirmScheduleRequest
{
    [Required]
    public int SessionId { get; set; }

    [Required]
    public int RinkId { get; set; }

    /// <summary>
    /// The subset of proposed games the admin wants to save.
    /// Admins may remove individual games from the preview before confirming.
    /// </summary>
    [Required]
    public List<ConfirmGameItem> Games { get; set; } = new();
}

public class ConfirmGameItem
{
    public DateTime GameDate { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public int RinkId { get; set; }
}
