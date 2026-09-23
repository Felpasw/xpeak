using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Xpeak.Api.Auth.Password;

/// <summary>
/// Tracks failed login attempts per identifier (email or username)
/// with a rolling 15-minute window. Backed by <see cref="IMemoryCache"/>
/// so it's per-node -- good enough for MVP single-instance; migrate
/// to Redis when we scale out.
/// </summary>
public sealed class FailedLoginCounter(IMemoryCache cache)
{
    private const int MaxFailuresPerWindow = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    public bool IsLocked(string identifier)
    {
        var key = Key(identifier);
        return cache.TryGetValue<int>(key, out var count) && count >= MaxFailuresPerWindow;
    }

    public void RecordFailure(string identifier)
    {
        var key = Key(identifier);
        var count = cache.TryGetValue<int>(key, out var existing) ? existing + 1 : 1;
        cache.Set(key, count, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Window,
        });
    }

    public void Clear(string identifier) => cache.Remove(Key(identifier));

    private static string Key(string identifier)
    {
        var normalized = identifier.Trim().ToLowerInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return "login-fail:" + Convert.ToHexString(hash);
    }
}
