using System.ComponentModel.DataAnnotations;

namespace Appointly.Api.Contracts;

public record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(128)] string FullName,
    [Required, MinLength(8), MaxLength(128)] string Password);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserResponse User);

public record UserResponse(Guid Id, string Email, string FullName, string Role);
