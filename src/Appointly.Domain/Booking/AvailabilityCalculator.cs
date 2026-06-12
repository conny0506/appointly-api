using Appointly.Domain.Entities;

namespace Appointly.Domain.Booking;

public readonly record struct TimeSlot(DateTimeOffset StartsAt, DateTimeOffset EndsAt);

/// <summary>Computes bookable slots for a provider/service on a given day.</summary>
public static class AvailabilityCalculator
{
    /// <summary>Candidate slots are generated on a fixed grid from the window start.</summary>
    public static readonly TimeSpan SlotStep = TimeSpan.FromMinutes(15);

    public static IReadOnlyList<TimeSlot> GetAvailableSlots(
        Provider provider,
        Service service,
        DateOnly day,
        IReadOnlyCollection<Appointment> providerAppointments,
        DateTimeOffset now,
        TimeSpan? utcOffset = null)
    {
        var offset = utcOffset ?? TimeSpan.Zero;
        var duration = TimeSpan.FromMinutes(service.DurationMinutes);
        var slots = new List<TimeSlot>();

        var windows = provider.WorkingHours
            .Where(w => w.DayOfWeek == day.DayOfWeek)
            .OrderBy(w => w.StartTime);

        foreach (var window in windows)
        {
            var cursor = new DateTimeOffset(day, window.StartTime, offset);
            var windowEnd = new DateTimeOffset(day, window.EndTime, offset);

            while (cursor + duration <= windowEnd)
            {
                var candidateEnd = cursor + duration;
                var isFree = cursor > now && !providerAppointments.Any(a =>
                    a.BlocksSlot && a.Overlaps(cursor, candidateEnd));

                if (isFree)
                    slots.Add(new TimeSlot(cursor, candidateEnd));

                cursor += SlotStep;
            }
        }

        return slots;
    }
}
