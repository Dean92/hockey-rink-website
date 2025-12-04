using System.ComponentModel.DataAnnotations;

namespace HockeyRinkAPI.Models
{
    public class SessionRegistration
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public int  SessionId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? Address { get; set; }
        
        [StringLength(100)]
        public string? City { get; set; }
        
        [StringLength(50)]
        public string? State { get; set; }
        
        [StringLength(20)]
        public string? ZipCode { get; set; }
        
        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public DateTime DateOfBirth { get; set; }
        
        [StringLength(20)]
        public string? Position { get; set; } // Forward, Defense, Forward/Defense, Goalie
        
        [Required]
        public DateTime RegistrationDate { get; set; }
        
        [Required]
        public decimal AmountPaid { get; set; }
        
        public string? PaymentStatus { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ApplicationUser? User { get; set; }
        public Session? Session { get; set; }
        public List<Payment> Payments { get; set; } = new();
    }
}
