using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HockeyRinkAPI.Models;

namespace HockeyRinkAPI.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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
            builder.Entity<Team>()
                .HasOne(t => t.League)
                .WithMany(l => l.Teams)
                .HasForeignKey(t => t.LeagueId);
            builder.Entity<Player>()
                .HasKey(p => new { p.UserId, p.TeamId });
            builder.Entity<SessionRegistration>()
                .HasOne(sr => sr.User)
                .WithMany()
                .HasForeignKey(sr => sr.UserId);
            builder.Entity<SessionRegistration>()
                .HasOne(sr => sr.Session)
                .WithMany(s => s.Registrations)
                .HasForeignKey(sr => sr.SessionId);
            builder.Entity<Payment>()
                .HasOne(p => p.Registration)
                .WithMany(sr => sr.Payments)
                .HasForeignKey(p => p.SessionRegistrationId);
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId);
        }
    }
}
