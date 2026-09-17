namespace Xpeak.Api.Tests.Support;

public sealed record UserDto(
    Guid Id,
    string Username,
    string Email,
    string? AvatarUrl,
    int Level,
    int Xp,
    DateTimeOffset CreatedAt);

public sealed record AuthResponseDto(UserDto User);
