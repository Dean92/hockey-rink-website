using System.ComponentModel.DataAnnotations;

namespace HockeyRinkAPI.Models.Requests;

public class UpdatePlayerRatingModel
{
    [Range(1.0, 5.0)]
    public decimal? Rating { get; set; }

    [StringLength(1000)]
    public string? PlayerNotes { get; set; }
}
