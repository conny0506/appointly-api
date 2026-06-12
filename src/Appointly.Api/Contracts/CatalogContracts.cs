using System.ComponentModel.DataAnnotations;

namespace Appointly.Api.Contracts;

public record ServiceRequest(
    [Required, MaxLength(128)] string Name,
    [MaxLength(1024)] string? Description,
    [Range(5, 480)] int DurationMinutes,
    [Range(0, 100_000)] decimal Price,
    bool Active = true);

public record ServiceResponse(
    Guid Id, string Name, string? Description, int DurationMinutes, decimal Price, bool Active);

public record WorkingHoursRequest(
    [Required] DayOfWeek DayOfWeek,
    [Required] TimeOnly StartTime,
    [Required] TimeOnly EndTime);

public record ProviderRequest(
    [Required, MaxLength(128)] string FullName,
    [MaxLength(128)] string? Title,
    bool Active = true);

public record ProviderResponse(
    Guid Id,
    string FullName,
    string? Title,
    bool Active,
    IReadOnlyList<ServiceResponse> Services,
    IReadOnlyList<WorkingHoursResponse> WorkingHours);

public record WorkingHoursResponse(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

public record SlotResponse(DateTimeOffset StartsAt, DateTimeOffset EndsAt);
