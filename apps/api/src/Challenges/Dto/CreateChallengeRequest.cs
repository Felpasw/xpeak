using Xpeak.Api.Challenges.Entities;

namespace Xpeak.Api.Challenges.Dto;

/// <summary>HTTP body of <c>POST /challenges</c>. The owner id
/// comes from the authenticated principal, never from the body.
/// <see cref="Tiers"/> is the optional progression ladder; null,
/// missing or an empty array all mean "no tiers".</summary>
public sealed record CreateChallengeRequest(
    string Title,
    string? Description,
    string? IconPublicId,
    ChallengeVisibility Visibility,
    int TargetLevel,
    decimal XpMultiplier,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? MemberCap,
    IReadOnlyList<CreateChallengeTierRequest>? Tiers = null);

/// <summary>Single tier inside <see cref="CreateChallengeRequest.Tiers"/>.
/// The service normalises <see cref="Title"/> with
/// <c>Trim()</c> before insert.</summary>
public sealed record CreateChallengeTierRequest(
    int MinLevel,
    string Title);
