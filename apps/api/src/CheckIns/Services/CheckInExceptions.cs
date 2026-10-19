namespace Xpeak.Api.CheckIns.Services;

/// <summary>
/// Thrown when <c>PublishAsync</c> is called against a check-in
/// that has already been published. Guards the publish path from
/// double-awarding XP.
/// </summary>
public sealed class CheckInAlreadyPublishedException(Guid checkInId)
    : Exception($"Check-in '{checkInId}' is already published.")
{
    public Guid CheckInId { get; } = checkInId;
}
