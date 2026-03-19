using System.ComponentModel.DataAnnotations;

namespace HockeyRinkAPI.Models.Requests;

public class ManualRegistrationModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(50)]
    public string? State { get; set; }

    [StringLength(20)]
    public string? ZipCode { get; set; }

    [Required]
    public DateTime DateOfBirth { get; set; }

    [StringLength(20)]
    public string? Position { get; set; }

    [Required]
    [Range(0, 10000)]
    public decimal AmountPaid { get; set; }

    [Range(0, 99)]
    public int? JerseyNumber { get; set; }
}
