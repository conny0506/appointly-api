using System.ComponentModel.DataAnnotations;

namespace Appointly.Api.Contracts;

public record ServiceRequest(
    [property: Required, MaxLength(128)] string Name,
    [property: MaxLength(1024)] string? Description,
    [property: Range(5, 480)] int DurationMinutes,
    [property: Range(0, 100_000)] decimal Price,
    bool Active = true);

public record ServiceResponse(
    Guid Id, string Name, string? Description, int DurationMinutes, decimal Price, bool Active);

public record WorkingHoursRequest(
    [property: Required] DayOfWeek DayOfWeek,
    [property: Required] TimeOnly StartTime,
    [property: Required] TimeOnly EndTime);

public record ProviderRequest(
    [property: Required, MaxLength(128)] string FullName,
    [property: MaxLength(128)] string? Title,
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
