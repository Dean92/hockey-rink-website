using System;
using System.Collections.Generic;

namespace HockeyRinkAPI.Models;

public partial class League
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal? EarlyBirdPrice { get; set; }

    public DateTime? RegistrationCloseDate { get; set; }

    public DateTime? RegistrationOpenDate { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EarlyBirdEndDate { get; set; }

    public decimal? RegularPrice { get; set; }

    public virtual ICollection<ApplicationUser> ApplicationUsers { get; set; } = new List<ApplicationUser>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
}
