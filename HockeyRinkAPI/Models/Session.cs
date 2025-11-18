using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HockeyRinkAPI.Models;

public class Session
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Session name is required")]
    [StringLength(100, ErrorMessage = "Session name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }

    [Range(0, 1000, ErrorMessage = "Fee must be between 0 and 1000")]
    public decimal Fee { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(1, 50, ErrorMessage = "Max players must be between 1 and 50")]
    public int MaxPlayers { get; set; } = 20;

    public DateTime? RegistrationOpenDate { get; set; }

    public DateTime? RegistrationCloseDate { get; set; }

    [Range(0, 1000, ErrorMessage = "Early bird price must be between 0 and 1000")]
    public decimal? EarlyBirdPrice { get; set; }

    public DateTime? EarlyBirdEndDate { get; set; }

    [Range(0, 1000, ErrorMessage = "Regular price must be between 0 and 1000")]
    public decimal? RegularPrice { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? LeagueId { get; set; } // Nullable temporarily

    public League? League { get; set; }

    public List<SessionRegistration> Registrations { get; set; } = new();
}
