namespace Xpeak.Api.Challenges.Services;

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
