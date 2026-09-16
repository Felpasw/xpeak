namespace Xpeak.Api.Auth.Core;

public sealed record UserResponse(
    Guid Id,
    string Username,
    string Email,
    string? AvatarUrl,
    int Level,
    int Xp,
    DateTimeOffset CreatedAt);
