using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Infrastructure;

namespace Xpeak.Api.Auth.Core;

/// <summary>
/// EF-only data access for <see cref="RevokedToken"/>. Zero business
/// rules: callers decide when/why to revoke.
/// </summary>
public sealed class RevokedTokenRepository(AppDbContext db)
{
    public async Task<bool> ExistsAsync(string jti, CancellationToken ct = default) =>
        await db.RevokedTokens.AsNoTracking().AnyAsync(t => t.Jti == jti, ct);

    public async Task AddAsync(
        string jti,
        Guid userId,
        DateTimeOffset expiresAt,
        CancellationToken ct = default)
    {
        if (await db.RevokedTokens.AnyAsync(t => t.Jti == jti, ct))
        {
            return;
        }

        db.RevokedTokens.Add(new RevokedToken
        {
            Jti = jti,
            UserId = userId,
            ExpiresAt = expiresAt,
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> DeleteExpiredAsync(DateTimeOffset now, CancellationToken ct = default) =>
        await db.RevokedTokens.Where(t => t.ExpiresAt < now).ExecuteDeleteAsync(ct);
}
