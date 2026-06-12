using System.Security.Claims;
using Appointly.Api.Contracts;
using Appointly.Api.Services;
using Appointly.Domain.Booking;
using Appointly.Domain.Entities;
using Appointly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Appointly.Api.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController(AppDbContext db, AppointmentService appointments) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AppointmentResponse>> Book(BookAppointmentRequest request)
    {
        var outcome = await appointments.BookAsync(
            CurrentUserId, request.ProviderId, request.ServiceId, request.StartsAt, request.Notes);

        if (!outcome.Succeeded)
            return UnprocessableEntity(ToProblem(outcome.Error));

        var a = outcome.Appointment!;
        return CreatedAtAction(nameof(Mine), new { id = a.Id }, ToResponse(a));
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<AppointmentResponse>>> Mine()
    {
        var list = await db.Appointments
            .Where(a => a.CustomerId == CurrentUserId)
            .Include(a => a.Provider)
            .Include(a => a.Service)
            .OrderByDescending(a => a.StartsAt)
            .ToListAsync();

        return Ok(list.Select(ToResponse).ToList());
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<AppointmentResponse>> Cancel(Guid id, CancelAppointmentRequest request)
    {
        var outcome = await appointments.CancelAsync(id, CurrentUserId, IsAdmin, request.Reason);

        if (!outcome.Succeeded)
            return UnprocessableEntity(ToProblem(outcome.Error));

        return Ok(ToResponse(outcome.Appointment!));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<AppointmentResponse>>> List(
        [FromQuery] Guid? providerId, [FromQuery] DateOnly? date)
    {
        var query = db.Appointments
            .Include(a => a.Provider)
            .Include(a => a.Service)
            .AsQueryable();

        if (providerId is { } pid)
            query = query.Where(a => a.ProviderId == pid);
        if (date is { } d)
        {
            var dayStart = new DateTimeOffset(d, TimeOnly.MinValue, TimeSpan.Zero);
            query = query.Where(a => a.StartsAt >= dayStart && a.StartsAt < dayStart.AddDays(1));
        }

        var list = await query.OrderBy(a => a.StartsAt).ToListAsync();
        return Ok(list.Select(ToResponse).ToList());
    }

    [HttpPost("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AppointmentResponse>> SetStatus(
        Guid id, [FromQuery] AppointmentStatus status)
    {
        var appointment = await db.Appointments
            .Include(a => a.Provider)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (appointment is null) return NotFound();

        appointment.Status = status;
        await db.SaveChangesAsync();
        return Ok(ToResponse(appointment));
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Token has no subject claim."));

    private bool IsAdmin => User.IsInRole(nameof(UserRole.Admin));

    private static AppointmentResponse ToResponse(Appointment a) => new(
        a.Id,
        a.ProviderId,
        a.Provider?.FullName ?? string.Empty,
        a.ServiceId,
        a.Service?.Name ?? string.Empty,
        a.StartsAt,
        a.EndsAt,
        a.Status.ToString(),
        a.Notes);

    private static ProblemDetails ToProblem(BookingError error) => new()
    {
        Title = error switch
        {
            BookingError.StartsInPast => "Appointment must start in the future.",
            BookingError.OutsideWorkingHours => "Requested time is outside the provider's working hours.",
            BookingError.OverlapsExistingAppointment => "The provider already has an appointment in this slot.",
            BookingError.ServiceInactive => "Service not found or inactive.",
            BookingError.ProviderInactive => "Provider not found or inactive.",
            BookingError.ProviderDoesNotOfferService => "Provider does not offer this service.",
            BookingError.CancellationWindowExpired =>
                $"Appointments can only be cancelled at least {BookingPolicy.CancellationCutoff.TotalHours:0} hours in advance.",
            BookingError.AppointmentNotCancellable => "Appointment cannot be cancelled in its current state.",
            _ => "Booking request was rejected.",
        },
    };
}
