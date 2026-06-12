namespace Appointly.Domain.Entities;

/// <summary>Weekly recurring availability window for a provider.</summary>
public class WorkingHours
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProviderId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public Provider? Provider { get; set; }
}
