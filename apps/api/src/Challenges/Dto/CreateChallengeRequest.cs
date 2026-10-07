using Xpeak.Api.Challenges.Entities;

namespace Xpeak.Api.Challenges.Dto;

/// <summary>HTTP body of <c>POST /challenges</c>. The owner id
/// comes from the authenticated principal, never from the body.</summary>
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
    IReadOnlyList<Guid> CategoryIds);
