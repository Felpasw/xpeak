using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Infrastructure;

namespace Xpeak.Api.Groups.Services;

public sealed class GroupConfigService(AppDbContext db) : IGroupConfigService
{
    public async Task<StreakConfig> GetStreakConfigAsync(Guid groupId, CancellationToken ct = default)
    {
        var config = await db.GroupConfigs
            .AsNoTracking()
            .SingleOrDefaultAsync(gc => gc.GroupId == groupId, ct);

        if (config is null)
        {
            throw new InvalidOperationException(
                $"No group_configs row for group {groupId}. Every group must have a config.");
        }

        return config.StreakConfig;
    }
}
