using System.ComponentModel.DataAnnotations;

namespace HockeyRinkAPI.Models.Requests;

public class UpdateUserProfileModel
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string State { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string ZipCode { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    [StringLength(20)]
    public string Position { get; set; } = string.Empty;

    [Range(1.0, 5.0)]
    public decimal? Rating { get; set; }

    [StringLength(1000)]
    public string? PlayerNotes { get; set; }

    public int? LeagueId { get; set; }
}
