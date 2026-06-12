using Appointly.Domain.Entities;

namespace Appointly.Domain.Booking;

public enum BookingError
{
    None = 0,
    StartsInPast,
    OutsideWorkingHours,
    OverlapsExistingAppointment,
    ServiceInactive,
    ProviderInactive,
    ProviderDoesNotOfferService,
    CancellationWindowExpired,
    AppointmentNotCancellable,
}

public readonly record struct BookingResult(bool IsValid, BookingError Error)
{
    public static readonly BookingResult Ok = new(true, BookingError.None);
    public static BookingResult Fail(BookingError error) => new(false, error);
}

/// <summary>
/// Pure domain rules for creating and cancelling appointments.
/// Stateless and framework-free so the rules stay unit-testable.
/// </summary>
public static class BookingPolicy
{
    /// <summary>Cancellations must happen at least this long before the start time.</summary>
    public static readonly TimeSpan CancellationCutoff = TimeSpan.FromHours(2);

    public static BookingResult CanBook(
        Provider provider,
        Service service,
        DateTimeOffset startsAt,
        IReadOnlyCollection<Appointment> providerAppointments,
        DateTimeOffset now)
    {
        if (!provider.Active)
            return BookingResult.Fail(BookingError.ProviderInactive);

        if (!service.Active)
            return BookingResult.Fail(BookingError.ServiceInactive);

        if (provider.Services.All(s => s.Id != service.Id))
            return BookingResult.Fail(BookingError.ProviderDoesNotOfferService);

        if (startsAt <= now)
            return BookingResult.Fail(BookingError.StartsInPast);

        var endsAt = startsAt.AddMinutes(service.DurationMinutes);

        if (!FitsWorkingHours(provider.WorkingHours, startsAt, endsAt))
            return BookingResult.Fail(BookingError.OutsideWorkingHours);

        var hasConflict = providerAppointments.Any(a => a.BlocksSlot && a.Overlaps(startsAt, endsAt));
        if (hasConflict)
            return BookingResult.Fail(BookingError.OverlapsExistingAppointment);

        return BookingResult.Ok;
    }

    public static BookingResult CanCancel(Appointment appointment, DateTimeOffset now)
    {
        if (!appointment.BlocksSlot)
            return BookingResult.Fail(BookingError.AppointmentNotCancellable);

        if (appointment.StartsAt - now < CancellationCutoff)
            return BookingResult.Fail(BookingError.CancellationWindowExpired);

        return BookingResult.Ok;
    }

    /// <summary>
    /// The whole appointment must fall inside a single working-hours window
    /// on the appointment's local day. Multi-day appointments are not supported.
    /// </summary>
    private static bool FitsWorkingHours(
        IReadOnlyCollection<WorkingHours> workingHours,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt)
    {
        if (startsAt.Date != endsAt.Date)
            return false;

        var day = startsAt.DayOfWeek;
        var start = TimeOnly.FromDateTime(startsAt.DateTime);
        var end = TimeOnly.FromDateTime(endsAt.DateTime);

        return workingHours.Any(w =>
            w.DayOfWeek == day && w.StartTime <= start && end <= w.EndTime);
    }
}
