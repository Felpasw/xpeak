namespace Xpeak.Api.Auth.Core;

/// <summary>
/// A JWT that has been revoked (logout). Presence of a row in this
/// table = token is invalid, even if not yet expired.
/// A <c>BackgroundService</c> sweeps rows where <see cref="ExpiresAt"/>
/// is in the past.
/// </summary>
public sealed class RevokedToken
{
    public required string Jti { get; init; }

    public Guid UserId { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }

    public DateTimeOffset RevokedAt { get; init; } = DateTimeOffset.UtcNow;
}
