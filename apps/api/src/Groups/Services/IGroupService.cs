namespace Xpeak.Api.Groups.Services;

/// <summary>
/// Only orchestration surface the Groups module exports. Auth endpoints
/// call <see cref="AddUserToGlobalAsync"/> right after creating a new
/// user so every account is a member of the global group from birth.
/// </summary>
public interface IGroupService
{
    /// <summary>
    /// Ensures the user has a membership row into <c>GroupIds.Global</c>.
    /// Idempotent — safe to call on every login or on retry after a
    /// partially-failed register.
    /// </summary>
    Task AddUserToGlobalAsync(Guid userId, CancellationToken ct = default);
}
