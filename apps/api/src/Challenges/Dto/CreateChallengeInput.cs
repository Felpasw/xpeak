using Xpeak.Api.Challenges.Entities;

namespace Xpeak.Api.Challenges.Dto;

/// <summary>Input to <c>IChallengeService.CreateAsync</c>. The owner
/// id comes from the authenticated principal, never from the body.
/// A challenge has no category whitelist — every check-in a member
/// posts inside <c>[starts_at, ends_at]</c> contributes to the
/// progress tally.</summary>
public sealed record CreateChallengeInput(
    string Title,
    string? Description,
    string? IconPublicId,
    ChallengeVisibility Visibility,
    int TargetLevel,
    decimal XpMultiplier,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? MemberCap);
