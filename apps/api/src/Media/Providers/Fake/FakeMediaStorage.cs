using Xpeak.Api.Media.Dto;

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

    public IReadOnlyList<string> PutCalls => _puts;
    public IReadOnlyList<string> GetCalls => _gets;
    public IReadOnlyList<string> DeleteCalls => _deletes;

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
        return Task.CompletedTask;
    }

    private static void EnsureKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
    }
}
