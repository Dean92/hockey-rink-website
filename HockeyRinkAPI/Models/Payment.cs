using System;
using System.Collections.Generic;

namespace HockeyRinkAPI.Models;

public partial class Payment
{
    public int Id { get; set; }

    public int SessionRegistrationId { get; set; }

    public decimal Amount { get; set; }

    public string? TransactionId { get; set; }

    public string? Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SessionRegistration SessionRegistration { get; set; } = null!;
}
