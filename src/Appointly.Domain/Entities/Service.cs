namespace Appointly.Domain.Entities;

public class Service
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public bool Active { get; set; } = true;

    public List<Provider> Providers { get; set; } = [];
}
