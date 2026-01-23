using Microsoft.AspNetCore.Identity;

namespace HockeyRinkAPI.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Basic profile info
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int? LeagueId { get; set; }
        public bool IsSubAvailable { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // Extended profile info (Week 7)
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Position { get; set; } // Forward, Defense, Forward/Defense, Goalie

        // Admin-only fields (Week 9)
        public decimal? Rating { get; set; } // Player skill rating (1.0 to 5.0)
        public string? PlayerNotes { get; set; } // Admin notes about player

        // Emergency contact (Week 10) - Collected during session registration
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }

        // Hockey registration (Week 10)
        public string? HockeyRegistrationNumber { get; set; }
        public string? HockeyRegistrationType { get; set; } // "USA Hockey", "AAU Hockey", or NULL

        // Manual registration support (Week 7)
        public bool IsManuallyRegistered { get; set; } = false;
        public string? PasswordSetupToken { get; set; }
        public DateTime? PasswordSetupTokenExpiry { get; set; }

        // Navigation properties
        public League? League { get; set; }
    }
}
