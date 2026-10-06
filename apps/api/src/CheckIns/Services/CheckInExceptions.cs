namespace Xpeak.Api.CheckIns.Services;

/// <summary>
/// Thrown when the caller submits a check-in against a
/// <c>category_id</c> that does not exist.
/// </summary>
public sealed class CategoryNotFoundException(Guid categoryId)
    : Exception($"Category '{categoryId}' does not exist.")
{
    public Guid CategoryId { get; } = categoryId;
}

/// <summary>
/// Thrown when the caller is not a member of the group that owns
/// the target category. Endpoint layer maps this to HTTP 403.
/// </summary>
public sealed class NotAGroupMemberException(Guid userId, Guid groupId)
    : Exception($"User '{userId}' is not a member of group '{groupId}'.")
{
    public Guid UserId { get; } = userId;
    public Guid GroupId { get; } = groupId;
}

/// <summary>
/// Thrown when <c>PublishAsync</c> is called against a check-in
/// that is already <see cref="Xpeak.Api.CheckIns.Entities.CheckInStatus.Published"/>.
/// Guards the publish path from double-awarding XP.
/// </summary>
public sealed class CheckInAlreadyPublishedException(Guid checkInId)
    : Exception($"Check-in '{checkInId}' is already published.")
{
    public Guid CheckInId { get; } = checkInId;
}
