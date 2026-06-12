using Appointly.Domain.Booking;
using Appointly.Domain.Entities;
using Appointly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Appointly.Api.Services;

public record BookingOutcome(Appointment? Appointment, BookingError Error)
{
    public bool Succeeded => Error == BookingError.None;
}

public class AppointmentService(AppDbContext db, TimeProvider clock)
{
    public async Task<BookingOutcome> BookAsync(
        Guid customerId, Guid providerId, Guid serviceId, DateTimeOffset startsAt, string? notes)
    {
        var provider = await db.Providers
            .Include(p => p.Services)
            .Include(p => p.WorkingHours)
            .FirstOrDefaultAsync(p => p.Id == providerId);
        var service = await db.Services.FindAsync(serviceId);

        if (provider is null)
            return new BookingOutcome(null, BookingError.ProviderInactive);
        if (service is null)
            return new BookingOutcome(null, BookingError.ServiceInactive);

        var endsAt = startsAt.AddMinutes(service.DurationMinutes);

        // The conflict check and the insert run in one transaction so two
        // concurrent requests cannot both pass validation for the same slot.
        await using var tx = await db.Database.BeginTransactionAsync();

        var dayStart = new DateTimeOffset(startsAt.UtcDateTime.Date.AddDays(-1), TimeSpan.Zero);
        var dayEnd = new DateTimeOffset(startsAt.UtcDateTime.Date.AddDays(2), TimeSpan.Zero);
        var nearbyAppointments = await db.Appointments
            .Where(a => a.ProviderId == providerId
                && a.StartsAt >= dayStart && a.StartsAt < dayEnd)
            .ToListAsync();

        var verdict = BookingPolicy.CanBook(
            provider, service, startsAt, nearbyAppointments, clock.GetUtcNow());
        if (!verdict.IsValid)
            return new BookingOutcome(null, verdict.Error);

        var appointment = new Appointment
        {
            CustomerId = customerId,
            ProviderId = providerId,
            ServiceId = serviceId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Notes = notes,
        };

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        appointment.Provider = provider;
        appointment.Service = service;
        return new BookingOutcome(appointment, BookingError.None);
    }

    public async Task<BookingOutcome> CancelAsync(
        Guid appointmentId, Guid actorId, bool actorIsAdmin, string? reason)
    {
        var appointment = await db.Appointments
            .Include(a => a.Provider)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == appointmentId
                && (actorIsAdmin || a.CustomerId == actorId));

        if (appointment is null)
            return new BookingOutcome(null, BookingError.AppointmentNotCancellable);

        // Admins can cancel at any time; customers must respect the cutoff.
        if (!actorIsAdmin)
        {
            var verdict = BookingPolicy.CanCancel(appointment, clock.GetUtcNow());
            if (!verdict.IsValid)
                return new BookingOutcome(null, verdict.Error);
        }
        else if (!appointment.BlocksSlot)
        {
            return new BookingOutcome(null, BookingError.AppointmentNotCancellable);
        }

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancellationReason = reason;
        await db.SaveChangesAsync();
        return new BookingOutcome(appointment, BookingError.None);
    }
}
