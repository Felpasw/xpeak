using Xpeak.Api.Challenges.Entities;

namespace Xpeak.Api.Challenges.Dto;

/// <summary>Wire representation of a <see cref="Challenge"/>.
/// Enum fields serialize as lowercase strings via the global
/// JsonStringEnumConverter.</summary>
public sealed record ChallengeResponse(
    Guid Id,
    Guid OwnerId,
    string Title,
    string? Description,
    string? IconPublicId,
    ChallengeVisibility Visibility,
    int TargetLevel,
    decimal XpMultiplier,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? MemberCap,
    ChallengeState State,
    DateTimeOffset CreatedAt)
{
    public static ChallengeResponse From(Challenge c) => new(
        c.Id,
        c.OwnerId,
        c.Title,
        c.Description,
        c.IconPublicId,
        c.Visibility,
        c.TargetLevel,
        c.XpMultiplier,
        c.StartsAt,
        c.EndsAt,
        c.MemberCap,
        c.State,
        c.CreatedAt);
}

/// <summary>Snapshot of a user's progress inside a challenge.</summary>
public sealed record MembershipProgressResponse(
    int Value,
    int Target,
    DateTimeOffset? AchievedAt)
{
    public static MembershipProgressResponse From(MembershipProgress p) =>
        new(p.Value, p.Target, p.AchievedAt);
}

/// <summary>Wire representation of a <see cref="ChallengeMembership"/>.</summary>
public sealed record MembershipResponse(
    Guid Id,
    Guid ChallengeId,
    Guid UserId,
    ChallengeMembershipRole Role,
    DateTimeOffset JoinedAt,
    DateTimeOffset? LeftAt,
    ChallengeMembershipResult? Result,
    MembershipProgressResponse Progress)
{
    public static MembershipResponse From(ChallengeMembership m) => new(
        m.Id,
        m.ChallengeId,
        m.UserId,
        m.Role,
        m.JoinedAt,
        m.LeftAt,
        m.Result,
        MembershipProgressResponse.From(m.Progress));
}

/// <summary>Payload of <c>POST /challenges</c>.</summary>
public sealed record CreateChallengeResponse(
    ChallengeResponse Challenge,
    MembershipResponse OwnerMembership);

/// <summary>Payload of <c>GET /challenges</c>.</summary>
public sealed record ListChallengesResponse(
    IReadOnlyList<ChallengeResponse> Challenges);

/// <summary>Payload of <c>GET /challenges/{id}</c>. Membership is
/// null when the caller views a public challenge they do not
/// belong to.</summary>
public sealed record GetChallengeResponse(
    ChallengeResponse Challenge,
    MembershipResponse? Membership);

/// <summary>Payload of <c>GET /challenges/{id}/progress</c>.</summary>
public sealed record ChallengeProgressResponse(
    MembershipProgressResponse Progress);
