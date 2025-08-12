using Microsoft.AspNetCore.Identity;

namespace HockeyRinkAPI.Models
{
    public class ApplicationUser :  IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int LeagueId { get; set; }
        public bool IsSubAvailable { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public League? League { get; set; }
    }
}
