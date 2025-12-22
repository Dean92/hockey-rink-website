using System;
using System.ComponentModel.DataAnnotations;

namespace HockeyRinkAPI.Models;

public class UpdateProfileModel
{
    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(50)]
    public string? State { get; set; }

    [StringLength(20)]
    public string? ZipCode { get; set; }

    [Phone]
    public string? Phone { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [StringLength(20)]
    public string? Position { get; set; }
}
