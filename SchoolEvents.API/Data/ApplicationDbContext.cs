using Microsoft.EntityFrameworkCore;
using SchoolEvents.API.Models;

namespace SchoolEvents.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<CalendarEvent> Events { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.MicrosoftId).IsUnique();
                entity.HasIndex(u => u.Email);
            });

            modelBuilder.Entity<CalendarEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.MicrosoftId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.StartTime);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.Events)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}