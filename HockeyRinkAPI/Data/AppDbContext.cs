using HockeyRinkAPI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HockeyRinkAPI.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<League> Leagues { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<SessionRegistration> SessionRegistrations { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder
                .Entity<Team>()
                .HasOne(t => t.Session)
                .WithMany(s => s.Teams)
                .HasForeignKey(t => t.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Team>()
                .HasIndex(t => new { t.SessionId, t.TeamName })
                .IsUnique();

            builder
                .Entity<Player>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .Entity<Player>()
                .HasOne(p => p.SessionRegistration)
                .WithMany()
                .HasForeignKey(p => p.SessionRegistrationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .Entity<Player>()
                .HasIndex(p => p.SessionRegistrationId)
                .IsUnique(); // Each registration can only be on one team

            builder
                .Entity<SessionRegistration>()
                .HasOne(sr => sr.User)
                .WithMany()
                .HasForeignKey(sr => sr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .Entity<SessionRegistration>()
                .HasOne(sr => sr.Session)
                .WithMany(s => s.SessionRegistrations)
                .HasForeignKey(sr => sr.SessionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .Entity<Payment>()
                .HasOne(p => p.SessionRegistration)
                .WithMany(sr => sr.Payments)
                .HasForeignKey(p => p.SessionRegistrationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Session>().Property(s => s.Fee).HasPrecision(10, 2);

            builder.Entity<Session>().Property(s => s.EarlyBirdPrice).HasPrecision(10, 2);

            builder.Entity<Session>().Property(s => s.RegularPrice).HasPrecision(10, 2);

            builder.Entity<League>().Property(l => l.EarlyBirdPrice).HasPrecision(10, 2);

            builder.Entity<League>().Property(l => l.RegularPrice).HasPrecision(10, 2);

            builder.Entity<Payment>().Property(p => p.Amount).HasPrecision(10, 2);

            builder.Entity<SessionRegistration>().Property(sr => sr.AmountPaid).HasPrecision(10, 2);

            builder.Entity<SessionRegistration>().Property(sr => sr.Rating).HasPrecision(3, 1);

            builder.Entity<SessionRegistration>().Property(sr => sr.DateOfBirth).HasColumnType("date");

            builder.Entity<SessionRegistration>().HasIndex(sr => sr.RegistrationDate);

            // Configure ApplicationUser LeagueId as optional
            builder
                .Entity<ApplicationUser>()
                .HasOne(u => u.League)
                .WithMany(l => l.ApplicationUsers)
                .HasForeignKey(u => u.LeagueId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
