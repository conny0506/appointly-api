using Appointly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Appointly.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<WorkingHours> WorkingHours => Set<WorkingHours>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.Email).HasMaxLength(256);
            b.Property(u => u.FullName).HasMaxLength(128);
            b.Property(u => u.Role).HasConversion<string>().HasMaxLength(16);
        });

        modelBuilder.Entity<Provider>(b =>
        {
            b.Property(p => p.FullName).HasMaxLength(128);
            b.Property(p => p.Title).HasMaxLength(128);
            b.HasMany(p => p.Services).WithMany(s => s.Providers);
        });

        modelBuilder.Entity<Service>(b =>
        {
            b.Property(s => s.Name).HasMaxLength(128);
            b.Property(s => s.Description).HasMaxLength(1024);
            b.Property(s => s.Price).HasPrecision(10, 2);
        });

        modelBuilder.Entity<WorkingHours>(b =>
        {
            b.HasOne(w => w.Provider)
                .WithMany(p => p.WorkingHours)
                .HasForeignKey(w => w.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(w => new { w.ProviderId, w.DayOfWeek });
        });

        modelBuilder.Entity<Appointment>(b =>
        {
            b.Property(a => a.Status).HasConversion<string>().HasMaxLength(16);
            b.Property(a => a.Notes).HasMaxLength(1024);
            b.Property(a => a.CancellationReason).HasMaxLength(512);

            b.HasOne(a => a.Customer)
                .WithMany(u => u.Appointments)
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(a => a.Provider)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(a => a.Service)
                .WithMany()
                .HasForeignKey(a => a.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Conflict checks query by provider and time range.
            b.HasIndex(a => new { a.ProviderId, a.StartsAt });
        });
    }
}
