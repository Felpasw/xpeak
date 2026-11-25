namespace Xpeak.Api.Social.Services;

/// <summary>
/// Single seam that decides whether a caller may read / interact
/// with a check-in. Reaction and comment endpoints call this once
/// per request and turn a <c>false</c> result into a 404 (not 403)
/// so the existence of a hidden check-in is never leaked.
///
/// MVP: <see cref="OpenCheckInVisibility"/> returns true for any
/// authenticated caller whose target check-in exists. Phase 12
/// swaps the implementation in DI to layer the friend-graph rule
/// without endpoint-level edits.
/// </summary>
public interface ICheckInVisibility
{
    Task<bool> CanSeeAsync(
        Guid callerId,
        Guid checkInId,
        CancellationToken ct = default);
}
