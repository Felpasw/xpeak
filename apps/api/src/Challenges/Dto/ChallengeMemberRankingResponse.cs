using Xpeak.Api.Users;

namespace Xpeak.Api.Challenges.Dto;

/// <summary>Row of the per-challenge leaderboard: the public card
/// of a member plus their challenge-scoped progress and, when the
/// challenge defines tiers, the tier they currently sit on.</summary>
public sealed record ChallengeMemberRankingResponse(
    PublicUserResponse User,
    MembershipProgressResponse Progress,
    ChallengeMemberTierResponse? Tier);

public sealed record ChallengeMemberTierResponse(
    Guid Id,
    int MinLevel,
    string Title);

public sealed record ListChallengeMembersResponse(
    IReadOnlyList<ChallengeMemberRankingResponse> Members);
