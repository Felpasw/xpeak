using Xpeak.Api.Challenges.Entities;

namespace Xpeak.Api.Challenges.Dto;

/// <summary>Input to <c>IChallengeService.CreateAsync</c>. The owner
/// id comes from the authenticated principal, never from the body.
/// A challenge has no category whitelist — every check-in a member
/// posts inside <c>[starts_at, ends_at]</c> contributes to the
/// progress tally. <see cref="Tiers"/> is the optional ladder of
/// progression tiers the owner declares at creation (null or empty
/// ⇒ no tiers).</summary>
public sealed record CreateChallengeInput(
    string Title,
    string? Description,
    string? IconPublicId,
    ChallengeVisibility Visibility,
    int TargetLevel,
    decimal XpMultiplier,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? MemberCap,
    IReadOnlyList<CreateChallengeTierInput>? Tiers = null);

/// <summary>Single tier inside <see cref="CreateChallengeInput.Tiers"/>.
/// Title is service-normalised with <c>Trim()</c> before insert.</summary>
public sealed record CreateChallengeTierInput(
    int MinLevel,
    string Title);
