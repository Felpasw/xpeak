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

/// <summary>Wire representation of the share-link
/// <see cref="ChallengeInvitation"/> that ships with every
/// challenge creation. The caller embeds <see cref="Id"/> in a
/// URL / deep link (<c>xpeak://challenges/join?invitation={Id}</c>)
/// to invite others.</summary>
public sealed record ChallengeInvitationResponse(
    Guid Id,
    Guid ChallengeId,
    Guid InvitedByUserId,
    ChallengeInvitationState State,
    DateTimeOffset CreatedAt)
{
    public static ChallengeInvitationResponse From(ChallengeInvitation i) =>
        new(i.Id, i.ChallengeId, i.InvitedByUserId, i.State, i.CreatedAt);
}

/// <summary>Payload of <c>POST /challenges</c>. Carries the default
/// share-link so the client can show "copy link" straight after
/// creation without a follow-up request.</summary>
public sealed record CreateChallengeResponse(
    ChallengeResponse Challenge,
    MembershipResponse OwnerMembership,
    ChallengeInvitationResponse ShareLink);

/// <summary>Payload of <c>GET /challenges</c>.</summary>
public sealed record ListChallengesResponse(
    IReadOnlyList<ChallengeResponse> Challenges);

/// <summary>Payload of <c>GET /challenges/{id}</c>. Membership is
/// null when the caller views a public challenge they do not
/// belong to. ShareLink is non-null only when the caller is the
/// owner — so the UI can render "copy share link" without a
/// second request.</summary>
public sealed record GetChallengeResponse(
    ChallengeResponse Challenge,
    MembershipResponse? Membership,
    ChallengeInvitationResponse? ShareLink);

/// <summary>Payload of <c>POST /challenges/join</c>. Mirrors
/// <see cref="GetChallengeResponse"/> but membership is always
/// non-null — the caller just joined.</summary>
public sealed record JoinChallengeResponse(
    ChallengeResponse Challenge,
    MembershipResponse Membership);

/// <summary>Payload of <c>GET /challenges/{id}/progress</c>.</summary>
public sealed record ChallengeProgressResponse(
    MembershipProgressResponse Progress);
