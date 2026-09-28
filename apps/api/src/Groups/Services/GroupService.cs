using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Infrastructure;

namespace Xpeak.Api.Groups.Services;

public sealed class GroupService(AppDbContext db) : IGroupService
{
    public async Task AddUserToGlobalAsync(Guid userId, CancellationToken ct = default)
    {
        var exists = await db.GroupMemberships
            .AsNoTracking()
            .AnyAsync(m => m.UserId == userId && m.GroupId == GroupIds.Global, ct);
        if (exists)
        {
            return;
        }

        db.GroupMemberships.Add(new GroupMembership
        {
            UserId = userId,
            GroupId = GroupIds.Global,
            JoinedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> IsMemberAsync(Guid userId, Guid groupId, CancellationToken ct = default) =>
        db.GroupMemberships
            .AsNoTracking()
            .AnyAsync(m => m.UserId == userId && m.GroupId == groupId, ct);
}
