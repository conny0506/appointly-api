using Appointly.Domain.Entities;
using Appointly.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Appointly.Infrastructure.Persistence;

public static class DbSeeder
{
    /// <summary>Creates the bootstrap admin and, when the catalog is empty, demo data.</summary>
    public static async Task SeedAsync(AppDbContext db, string adminEmail, string adminPassword)
    {
        if (!await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
        {
            db.Users.Add(new User
            {
                Email = adminEmail,
                FullName = "Administrator",
                PasswordHash = PasswordHasher.Hash(adminPassword),
                Role = UserRole.Admin,
            });
        }

        if (!await db.Providers.AnyAsync())
        {
            var haircut = new Service { Name = "Haircut", DurationMinutes = 30, Price = 25m };
            var coloring = new Service { Name = "Hair Coloring", DurationMinutes = 90, Price = 80m };

            var weekdays = new[]
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday,
            };

            var provider = new Provider
            {
                FullName = "Alex Carter",
                Title = "Senior Stylist",
                Services = [haircut, coloring],
                WorkingHours = weekdays.Select(d => new WorkingHours
                {
                    DayOfWeek = d,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(17, 0),
                }).ToList(),
            };

            db.Providers.Add(provider);
        }

        await db.SaveChangesAsync();
    }
}
