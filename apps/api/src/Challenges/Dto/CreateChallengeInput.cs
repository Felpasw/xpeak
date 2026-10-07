using Xpeak.Api.Challenges.Entities;

namespace Xpeak.Api.Challenges.Dto;

/// <summary>Input to <c>IChallengeService.CreateAsync</c>. The owner
/// id comes from the authenticated principal, never from the body.
/// <see cref="CategoryIds"/> is the whitelist of category rows that
/// count toward the win condition; duplicates are silently
/// deduplicated, empty list is rejected.</summary>
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
    IReadOnlyList<Guid> CategoryIds);
