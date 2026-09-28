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
