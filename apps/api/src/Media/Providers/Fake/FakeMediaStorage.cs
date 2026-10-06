using System.Collections.Concurrent;
using Xpeak.Api.Media.Dto;
using Xpeak.Api.Media.Entities;

namespace Xpeak.Api.Media.Providers.Fake;

/// <summary>
/// In-memory <see cref="IMediaStorage"/> that hands out deterministic
/// URLs and records every call. Meant for tests and local development
/// (running the API without Cloudinary credentials).
/// </summary>
public sealed class FakeMediaStorage : IMediaStorage
{
    private readonly List<string> _puts = [];
    private readonly List<string> _gets = [];
    private readonly List<string> _deletes = [];
    private readonly ConcurrentDictionary<string, MediaMetadata> _metadata = new();

    public IReadOnlyList<string> PutCalls => _puts;
    public IReadOnlyList<string> GetCalls => _gets;
    public IReadOnlyList<string> DeleteCalls => _deletes;

    /// <summary>Test hook: pre-load the metadata that
    /// <see cref="GetMetadataAsync"/> should report for <paramref name="key"/>.
    /// Simulates the state after a successful upload.</summary>
    public void SeedMetadata(string key, MediaMetadata metadata) =>
        _metadata[key] = metadata;

    public Task<PresignedUpload> PresignedPutUrlAsync(
        string key,
        PresignOptions options,
        CancellationToken ct)
    {
        EnsureKey(key);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.ByteSize);

        _puts.Add(key);
        var ttl = (int)options.Ttl.TotalSeconds;
        var upload = new PresignedUpload($"https://fake.local/put/{key}?ttl={ttl}", options.Ttl);
        return Task.FromResult(upload);
    }

    public Task<string> PresignedGetUrlAsync(string key, TimeSpan ttl, CancellationToken ct)
    {
        EnsureKey(key);

        _gets.Add(key);
        return Task.FromResult($"https://fake.local/get/{key}?ttl={(int)ttl.TotalSeconds}");
    }

    public Task DeleteAsync(string key, CancellationToken ct)
    {
        EnsureKey(key);

        _deletes.Add(key);
        _metadata.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public string BuildThumbUrl(string key, MediaKind kind)
    {
        EnsureKey(key);
        return $"https://fake.local/thumb/{key}?kind={kind.ToString().ToLowerInvariant()}";
    }

    public Task<MediaMetadata?> GetMetadataAsync(string key, MediaKind kind, CancellationToken ct)
    {
        EnsureKey(key);

        _metadata.TryGetValue(key, out var meta);
        return Task.FromResult<MediaMetadata?>(meta);
    }

    private static void EnsureKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
    }
}
