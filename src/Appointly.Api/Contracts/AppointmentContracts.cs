using System.ComponentModel.DataAnnotations;

namespace Appointly.Api.Contracts;

public record BookAppointmentRequest(
    [Required] Guid ProviderId,
    [Required] Guid ServiceId,
    [Required] DateTimeOffset StartsAt,
    [MaxLength(1024)] string? Notes);

public record CancelAppointmentRequest([MaxLength(512)] string? Reason);

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
