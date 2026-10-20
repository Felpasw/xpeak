namespace Xpeak.Api.Challenges.Dto;

/// <summary>HTTP body of <c>POST /challenges/join</c>. The caller
/// posts the share-link invitation id they received through a URL
/// or a deep link.</summary>
public sealed record JoinChallengeRequest(Guid InvitationId);
