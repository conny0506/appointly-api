using Appointly.Domain.Booking;
using Appointly.Domain.Entities;

namespace Appointly.Tests.Domain;

public class AvailabilityCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Monday = new(2030, 1, 7);

    private static (Provider, Service) ProviderWithMorningShift(int durationMinutes = 30)
    {
        var service = new Service { Name = "Haircut", DurationMinutes = durationMinutes, Price = 25m };
        var provider = new Provider
        {
            FullName = "Alex Carter",
            Services = [service],
            WorkingHours =
            [
                new WorkingHours
                {
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(10, 0),
                },
            ],
        };
        return (provider, service);
    }

    [Fact]
    public void Generates_slots_on_a_15_minute_grid_within_the_window()
    {
        var (provider, service) = ProviderWithMorningShift();

        var slots = AvailabilityCalculator.GetAvailableSlots(provider, service, Monday, [], Now);

        // 09:00-10:00 window, 30min service, 15min grid -> 09:00, 09:15, 09:30
        Assert.Equal(3, slots.Count);
        Assert.Equal(new TimeOnly(9, 0), TimeOnly.FromDateTime(slots[0].StartsAt.DateTime));
        Assert.Equal(new TimeOnly(9, 30), TimeOnly.FromDateTime(slots[^1].StartsAt.DateTime));
    }

    [Fact]
    public void Excludes_slots_that_overlap_existing_appointments()
    {
        var (provider, service) = ProviderWithMorningShift();
        var booked = new Appointment
        {
            ProviderId = provider.Id,
            CustomerId = Guid.NewGuid(),
            ServiceId = service.Id,
            StartsAt = new DateTimeOffset(2030, 1, 7, 9, 0, 0, TimeSpan.Zero),
            EndsAt = new DateTimeOffset(2030, 1, 7, 9, 30, 0, TimeSpan.Zero),
            Status = AppointmentStatus.Confirmed,
        };

        var slots = AvailabilityCalculator.GetAvailableSlots(
            provider, service, Monday, [booked], Now);

        // Only 09:30 remains: 09:00 and 09:15 collide with the booked 09:00-09:30.
        var single = Assert.Single(slots);
        Assert.Equal(new TimeOnly(9, 30), TimeOnly.FromDateTime(single.StartsAt.DateTime));
    }

    [Fact]
    public void Returns_empty_when_day_has_no_working_hours()
    {
        var (provider, service) = ProviderWithMorningShift();
        var sunday = new DateOnly(2030, 1, 6);

        var slots = AvailabilityCalculator.GetAvailableSlots(provider, service, sunday, [], Now);

        Assert.Empty(slots);
    }

    [Fact]
    public void Excludes_slots_in_the_past()
    {
        var (provider, service) = ProviderWithMorningShift();
        var nowDuringShift = new DateTimeOffset(2030, 1, 7, 9, 20, 0, TimeSpan.Zero);

        var slots = AvailabilityCalculator.GetAvailableSlots(
            provider, service, Monday, [], nowDuringShift);

        // 09:00 and 09:15 already started; only 09:30 is bookable.
        var single = Assert.Single(slots);
        Assert.Equal(new TimeOnly(9, 30), TimeOnly.FromDateTime(single.StartsAt.DateTime));
    }
}
