using Xpeak.Api.Progression.Entities;

namespace Xpeak.Api.Progression.Repositories;

/// <summary>
/// Read surface for <see cref="Category"/>. Every call is scoped by
/// <c>groupId</c> — no cross-group reads. Writes (create / update /
/// deactivate) land on the admin surface in Phase 9 or the
/// group-owner CRUD in Phase 17; not exposed here yet.
/// </summary>
public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListActiveAsync(
        Guid groupId,
        CancellationToken ct = default);

    Task<Category?> GetBySlugAsync(
        Guid groupId,
        string slug,
        CancellationToken ct = default);
}
