namespace Appointly.Domain.Entities;

/// <summary>A staff member who delivers services and owns a weekly schedule.</summary>
public class Provider
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string FullName { get; set; }
    public string? Title { get; set; }
    public bool Active { get; set; } = true;

    public List<Service> Services { get; set; } = [];
    public List<WorkingHours> WorkingHours { get; set; } = [];
    public List<Appointment> Appointments { get; set; } = [];
}
