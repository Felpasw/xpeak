namespace Xpeak.Api.Challenges.Entities;

/// <summary>
/// A time-boxed goal. Owned by a user, joined by users via
/// <c>challenge_memberships</c>. The target lives on the challenge
/// itself (same for every member); per-member progress is cached on
/// each membership row. Every check-in a member posts inside the
/// window contributes — there is no category whitelist. Every
/// challenge is inherently group-shaped (owner is the first
/// member); the distinction between "solo", "friend" and "group"
/// is just <see cref="MemberCap"/>.
/// </summary>
public sealed class Challenge
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Cloudinary <c>public_id</c> of the challenge badge /
    /// cover image. Nullable — the UI falls back to a generated
    /// placeholder when unset. Same convention as
    /// <see cref="Progression.Entities.Category.IconPublicId"/>.</summary>
    public string? IconPublicId { get; set; }

    public ChallengeVisibility Visibility { get; set; } = ChallengeVisibility.Private;

    /// <summary>Target level the member must reach from zero within
    /// the challenge window. The required XP is derived from the
    /// global level curve (<c>LevelCurve.XpForLevel</c>) applied to
    /// the challenge-scoped XP tally. Positive integer.</summary>
    public int TargetLevel { get; set; } = 1;

    public decimal XpMultiplier { get; set; } = 1.0m;

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }

    /// <summary>Null = no cap (open challenge). <c>2</c> is the
    /// classic "friend" shape. Soft-enforced by the join service,
    /// not by a DB constraint.</summary>
    public int? MemberCap { get; set; }

    public ChallengeState State { get; set; } = ChallengeState.Active;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
