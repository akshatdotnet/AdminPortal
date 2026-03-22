namespace Identity.Application.DTOs;

public sealed record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string UserId,
    string Email,
    string FullName,
    string Role);

public sealed record UserProfileDto(
    Guid Id, string Email, string FirstName, string LastName,
    string PhoneNumber, string Role, bool EmailConfirmed, bool IsActive);
