using System;
using System.Collections.Generic;

namespace HockeyRinkAPI.Models;

public partial class SessionRegistration
{
    public int Id { get; set; }

    public string? UserId { get; set; }

    public int SessionId { get; set; }

    public string? PaymentStatus { get; set; }

    public DateTime PaymentDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Address { get; set; }

    public decimal AmountPaid { get; set; }

    public string? City { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public string Email { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Position { get; set; }

    public DateTime RegistrationDate { get; set; }

    public string? State { get; set; }

    public string? ZipCode { get; set; }

    public decimal? Rating { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Session Session { get; set; } = null!;

    public virtual ApplicationUser? User { get; set; }
}
