using System.ComponentModel.DataAnnotations;

namespace Appointly.Api.Contracts;

public record RegisterRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required, MaxLength(128)] string FullName,
    [property: Required, MinLength(8), MaxLength(128)] string Password);

public record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserResponse User);

public record UserResponse(Guid Id, string Email, string FullName, string Role);
