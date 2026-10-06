using Microsoft.Extensions.Caching.Memory;

namespace Xpeak.Api.Media.Services;

/// <summary>
/// In-process <see cref="IPendingMediaCache"/> backed by
/// <see cref="IMemoryCache"/>. Fine for single-instance dev/MVP;
/// swap for a distributed cache (Redis) when the API scales
/// horizontally — otherwise a request that lands on a different
/// pod than the presign one would fail to confirm.
/// </summary>
public sealed class PendingMediaCache(IMemoryCache cache) : IPendingMediaCache
{
    public static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);

    public void Register(string storageKey, Guid checkInId)
    {
        cache.Set(KeyFor(storageKey), checkInId, Ttl);
    }

    public Guid? Consume(string storageKey)
    {
        var slot = KeyFor(storageKey);
        if (!cache.TryGetValue(slot, out Guid checkInId))
        {
            return null;
        }
        cache.Remove(slot);
        return checkInId;
    }

    private static string KeyFor(string storageKey) => $"pending-media:{storageKey}";
}
