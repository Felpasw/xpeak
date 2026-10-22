namespace Xpeak.Api.Challenges.Entities;

/// <summary>
/// Owner-defined progression tier inside a single challenge.
/// Members cross tiers as their challenge-scoped
/// <see cref="MembershipProgress.Value"/> climbs. A challenge
/// carries 0..10 tiers; the hard cap lives at the service layer.
/// Tiers are frozen at challenge creation — no edit after.
/// Distinct from Phase 8 themed titles (which name the user's
/// global level across the whole app). Order comes from
/// <see cref="MinLevel"/> ascending — there is no separate
/// position column.
/// </summary>
public sealed class ChallengeTier
{
    public Guid Id { get; set; }

    public Guid ChallengeId { get; set; }

    public int MinLevel { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Cloudinary public_id (storage key) of the tier
    /// artwork. Null = no image, UI falls back to a dot. Written
    /// by the create flow after the mobile presigns + uploads the
    /// bytes to Cloudinary; the public URL is derived at read time
    /// via <see cref="Media.IMediaStorage.BuildThumbUrl"/>.</summary>
    public string? ImageStorageKey { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
