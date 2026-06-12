using Appointly.Domain.Booking;
using Appointly.Domain.Entities;

namespace Appointly.Tests.Domain;

public class BookingPolicyTests
{
    // Monday 2030-01-07 — far future so "now" checks never interfere.
    private static readonly DateTimeOffset Now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset MondayAt10 = new(2030, 1, 7, 10, 0, 0, TimeSpan.Zero);

    private static Service Haircut() => new()
    {
        Name = "Haircut",
        DurationMinutes = 30,
        Price = 25m,
    };

    private static Provider ProviderWith(Service service) => new()
    {
        FullName = "Alex Carter",
        Services = [service],
        WorkingHours =
        [
            new WorkingHours
            {
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(17, 0),
            },
        ],
    };

    [Fact]
    public void Accepts_a_valid_booking()
    {
        var service = Haircut();
        var provider = ProviderWith(service);

        var result = BookingPolicy.CanBook(provider, service, MondayAt10, [], Now);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Rejects_booking_in_the_past()
    {
        var service = Haircut();
        var provider = ProviderWith(service);

        var result = BookingPolicy.CanBook(provider, service, Now.AddHours(-1), [], Now);

        Assert.Equal(BookingError.StartsInPast, result.Error);
    }

    [Theory]
    [InlineData(8, 30)]   // starts before opening
    [InlineData(16, 45)]  // 30min service would end 17:15, after closing
    [InlineData(20, 0)]   // evening, fully outside
    public void Rejects_booking_outside_working_hours(int hour, int minute)
    {
        var service = Haircut();
        var provider = ProviderWith(service);
        var start = new DateTimeOffset(2030, 1, 7, hour, minute, 0, TimeSpan.Zero);

        var result = BookingPolicy.CanBook(provider, service, start, [], Now);

        Assert.Equal(BookingError.OutsideWorkingHours, result.Error);
    }

    [Fact]
    public void Rejects_booking_on_a_day_without_working_hours()
    {
        var service = Haircut();
        var provider = ProviderWith(service);
        var sunday = new DateTimeOffset(2030, 1, 6, 10, 0, 0, TimeSpan.Zero);

        var result = BookingPolicy.CanBook(provider, service, sunday, [], Now);

        Assert.Equal(BookingError.OutsideWorkingHours, result.Error);
    }

    [Theory]
    [InlineData(0)]    // identical start
    [InlineData(-15)]  // existing appointment started 15min earlier and still runs
    [InlineData(15)]   // new booking starts mid-way through existing one
    public void Rejects_overlapping_booking(int offsetMinutes)
    {
        var service = Haircut();
        var provider = ProviderWith(service);
        var existing = new Appointment
        {
            ProviderId = provider.Id,
            CustomerId = Guid.NewGuid(),
            ServiceId = service.Id,
            StartsAt = MondayAt10.AddMinutes(offsetMinutes),
            EndsAt = MondayAt10.AddMinutes(offsetMinutes + 30),
            Status = AppointmentStatus.Confirmed,
        };

        var result = BookingPolicy.CanBook(provider, service, MondayAt10, [existing], Now);

        Assert.Equal(BookingError.OverlapsExistingAppointment, result.Error);
    }

    [Fact]
    public void Back_to_back_appointments_do_not_conflict()
    {
        var service = Haircut();
        var provider = ProviderWith(service);
        var existing = new Appointment
        {
            ProviderId = provider.Id,
            CustomerId = Guid.NewGuid(),
            ServiceId = service.Id,
            StartsAt = MondayAt10.AddMinutes(-30),
            EndsAt = MondayAt10, // ends exactly when the new one starts
            Status = AppointmentStatus.Confirmed,
        };

        var result = BookingPolicy.CanBook(provider, service, MondayAt10, [existing], Now);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Cancelled_appointments_do_not_block_the_slot()
    {
        var service = Haircut();
        var provider = ProviderWith(service);
        var cancelled = new Appointment
        {
            ProviderId = provider.Id,
            CustomerId = Guid.NewGuid(),
            ServiceId = service.Id,
            StartsAt = MondayAt10,
            EndsAt = MondayAt10.AddMinutes(30),
            Status = AppointmentStatus.Cancelled,
        };

        var result = BookingPolicy.CanBook(provider, service, MondayAt10, [cancelled], Now);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Rejects_service_the_provider_does_not_offer()
    {
        var service = Haircut();
        var provider = ProviderWith(service);
        var otherService = new Service { Name = "Massage", DurationMinutes = 60, Price = 50m };

        var result = BookingPolicy.CanBook(provider, otherService, MondayAt10, [], Now);

        Assert.Equal(BookingError.ProviderDoesNotOfferService, result.Error);
    }

    [Fact]
    public void Cancellation_allowed_before_cutoff()
    {
        var appointment = new Appointment
        {
            StartsAt = Now.AddHours(3),
            EndsAt = Now.AddHours(3.5),
            Status = AppointmentStatus.Confirmed,
        };

        var result = BookingPolicy.CanCancel(appointment, Now);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Cancellation_rejected_inside_cutoff_window()
    {
        var appointment = new Appointment
        {
            StartsAt = Now.AddMinutes(90), // less than the 2h cutoff
            EndsAt = Now.AddMinutes(120),
            Status = AppointmentStatus.Confirmed,
        };

        var result = BookingPolicy.CanCancel(appointment, Now);

        Assert.Equal(BookingError.CancellationWindowExpired, result.Error);
    }

    [Fact]
    public void Completed_appointment_cannot_be_cancelled()
    {
        var appointment = new Appointment
        {
            StartsAt = Now.AddHours(5),
            EndsAt = Now.AddHours(6),
            Status = AppointmentStatus.Completed,
        };

        var result = BookingPolicy.CanCancel(appointment, Now);

        Assert.Equal(BookingError.AppointmentNotCancellable, result.Error);
    }
}
