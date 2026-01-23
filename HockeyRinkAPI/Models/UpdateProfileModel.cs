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

    // Emergency Contact (Week 10) - Optional, can be updated from profile
    [StringLength(100)]
    public string? EmergencyContactName { get; set; }

    [StringLength(20)]
    public string? EmergencyContactPhone { get; set; }

    // Hockey Registration (Week 10)
    [StringLength(50)]
    public string? HockeyRegistrationNumber { get; set; }

    [StringLength(20)]
    public string? HockeyRegistrationType { get; set; } // "USA Hockey", "AAU Hockey", or null
}
