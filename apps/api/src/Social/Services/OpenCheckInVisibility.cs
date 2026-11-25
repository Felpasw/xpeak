using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Infrastructure;

namespace Xpeak.Api.Social.Services;

/// <summary>
/// MVP implementation of <see cref="ICheckInVisibility"/>. Returns
/// true for any authenticated caller whose target check-in exists.
/// Phase 12 replaces this implementation in DI with the friend-graph
/// rule; endpoint code stays untouched.
/// </summary>
public sealed class OpenCheckInVisibility(AppDbContext db) : ICheckInVisibility
{
    public async Task<bool> CanSeeAsync(
        Guid callerId,
        Guid checkInId,
        CancellationToken ct = default) =>
        await db.CheckIns
            .AsNoTracking()
            .AnyAsync(c => c.Id == checkInId, ct);
}
