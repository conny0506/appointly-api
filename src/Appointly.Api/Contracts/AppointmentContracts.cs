using System.ComponentModel.DataAnnotations;

namespace Appointly.Api.Contracts;

public record BookAppointmentRequest(
    [property: Required] Guid ProviderId,
    [property: Required] Guid ServiceId,
    [property: Required] DateTimeOffset StartsAt,
    [property: MaxLength(1024)] string? Notes);

public record CancelAppointmentRequest([property: MaxLength(512)] string? Reason);

public record AppointmentResponse(
    Guid Id,
    Guid ProviderId,
    string ProviderName,
    Guid ServiceId,
    string ServiceName,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Status,
    string? Notes);
