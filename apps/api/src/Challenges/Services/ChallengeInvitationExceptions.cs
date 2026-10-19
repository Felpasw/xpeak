namespace Xpeak.Api.Challenges.Services;

/// <summary>Thrown when an invitation operation targets an invitation
/// row that does not exist (bad id, hard-deleted). Endpoint layer
/// maps to HTTP 404.</summary>
public sealed class InvitationNotFoundException(Guid invitationId)
    : Exception($"Invitation '{invitationId}' does not exist.")
{
    public Guid InvitationId { get; } = invitationId;
}

/// <summary>Thrown when the invitation exists but is no longer in
/// a state that accepts joins (revoked, consumed, or already
/// accepted/rejected). Endpoint layer maps to HTTP 409.</summary>
public sealed class InvitationNotActiveException(Guid invitationId, Entities.ChallengeInvitationState state)
    : Exception($"Invitation '{invitationId}' is not pending (state: {state}).")
{
    public Guid InvitationId { get; } = invitationId;
    public Entities.ChallengeInvitationState State { get; } = state;
}

/// <summary>Thrown when a join would push the challenge past its
/// <c>MemberCap</c>. Endpoint layer maps to HTTP 409.</summary>
public sealed class MemberCapReachedException(Guid challengeId, int cap)
    : Exception($"Challenge '{challengeId}' already has {cap} members (the configured cap).")
{
    public Guid ChallengeId { get; } = challengeId;
    public int Cap { get; } = cap;
}

/// <summary>Thrown when the caller is already a member of the
/// target challenge. Endpoint layer maps to HTTP 409.</summary>
public sealed class AlreadyMemberException(Guid challengeId, Guid userId)
    : Exception($"User '{userId}' is already a member of challenge '{challengeId}'.")
{
    public Guid ChallengeId { get; } = challengeId;
    public Guid UserId { get; } = userId;
}

/// <summary>Thrown when an invitation operation targets a challenge
/// that does not exist. Endpoint layer maps to HTTP 404.</summary>
public sealed class ChallengeNotFoundException(Guid challengeId)
    : Exception($"Challenge '{challengeId}' does not exist.")
{
    public Guid ChallengeId { get; } = challengeId;
}

/// <summary>Thrown when an invitation operation is attempted by a
/// user who is not a member of the challenge. Endpoint layer maps
/// to HTTP 403.</summary>
public sealed class CallerNotAMemberException(Guid challengeId, Guid callerId)
    : Exception($"User '{callerId}' is not a member of challenge '{challengeId}'.")
{
    public Guid ChallengeId { get; } = challengeId;
    public Guid CallerId { get; } = callerId;
}

/// <summary>Thrown when an invitation operation targets a challenge
/// that is no longer accepting joins (anything other than
/// <see cref="Entities.ChallengeState.Active"/>). Endpoint layer
/// maps to HTTP 409.</summary>
public sealed class ChallengeNotActiveException(Guid challengeId, Entities.ChallengeState state)
    : Exception($"Challenge '{challengeId}' is not active (state: {state}).")
{
    public Guid ChallengeId { get; } = challengeId;
    public Entities.ChallengeState State { get; } = state;
}
