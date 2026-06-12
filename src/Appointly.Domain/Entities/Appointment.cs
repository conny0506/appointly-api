namespace Appointly.Domain.Entities;

public enum AppointmentStatus
{
    Pending = 0,
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3,
    NoShow = 4,
}

public class Appointment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Guid ProviderId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? Customer { get; set; }
    public Provider? Provider { get; set; }
    public Service? Service { get; set; }

    /// <summary>Statuses that still occupy the provider's calendar slot.</summary>
    public bool BlocksSlot => Status is AppointmentStatus.Pending or AppointmentStatus.Confirmed;

    public bool Overlaps(DateTimeOffset start, DateTimeOffset end) =>
        StartsAt < end && start < EndsAt;
}
