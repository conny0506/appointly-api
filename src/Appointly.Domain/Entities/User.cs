namespace Appointly.Domain.Entities;

public enum UserRole
{
    Customer = 0,
    Admin = 1,
}

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Email { get; set; }
    public required string FullName { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<Appointment> Appointments { get; set; } = [];
}
