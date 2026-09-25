using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;

namespace Xpeak.Api.Progression.Repositories;

public sealed class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> ListActiveAsync(
        Guid groupId,
        CancellationToken ct = default)
    {
        return await db.Categories
            .AsNoTracking()
            .Where(c => c.GroupId == groupId && c.Active)
            .OrderBy(c => c.Slug)
            .ToListAsync(ct);
    }

    public Task<Category?> GetBySlugAsync(
        Guid groupId,
        string slug,
        CancellationToken ct = default)
    {
        // `slug` column is `citext`, so the comparison is
        // case-insensitive at the DB level — no need to normalize here.
        return db.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.GroupId == groupId && c.Slug == slug, ct);
    }

    public Task<Category?> GetByIdAsync(Guid categoryId, CancellationToken ct = default) =>
        db.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == categoryId, ct);
}
