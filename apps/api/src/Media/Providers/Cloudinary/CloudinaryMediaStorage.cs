using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Xpeak.Api.Media.Dto;
using Xpeak.Api.Media.Entities;

namespace Xpeak.Api.Media.Providers.Cloudinary;

/// <summary>
/// Cloudinary-backed <see cref="IMediaStorage"/>. Signs upload and delivery
/// URLs locally (Cloudinary uses <c>sha1(sorted-params + api_secret)</c>) and
/// calls the destroy endpoint for deletes. The signature algorithm and URL
/// shape follow <see href="https://cloudinary.com/documentation/upload_images#generating_authentication_signatures"/>.
/// </summary>
public sealed class CloudinaryMediaStorage(
    IOptions<CloudinaryOptions> options,
    HttpClient http,
    TimeProvider time) : IMediaStorage
{
    private const string DeliveryHost = "https://res.cloudinary.com";
    private static readonly TimeSpan SignatureCeiling = TimeSpan.FromHours(1);

    private readonly CloudinaryOptions _opts = options.Value;

    public Task<PresignedUpload> PresignedPutUrlAsync(
        string key,
        PresignOptions options,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.ByteSize);

        var resource = ResourceKindFor(options.ContentType);
        var timestamp = time.GetUtcNow().ToUnixTimeSeconds();

        // Cloudinary signs everything except api_key, file, cloud_name and resource_type.
        var signedParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["public_id"] = key,
            ["timestamp"] = timestamp.ToString(),
        };
        var signature = Sign(signedParams);

        var query = new List<string>
        {
            $"api_key={Uri.EscapeDataString(_opts.ApiKey)}",
            $"timestamp={timestamp}",
            $"signature={signature}",
            $"public_id={Uri.EscapeDataString(key)}",
        };
        var url = $"{_opts.ApiBaseUrl}/v1_1/{_opts.CloudName}/{resource}/upload?{string.Join('&', query)}";

        var ttl = options.Ttl > SignatureCeiling ? SignatureCeiling : options.Ttl;
        return Task.FromResult(new PresignedUpload(url, ttl));
    }

    public Task<string> PresignedGetUrlAsync(string key, TimeSpan ttl, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        // Signed delivery URL: sha1("<public_id>" + api_secret), first 8 chars, base64url.
        var toSign = $"{key}{_opts.ApiSecret}";
        var digest = SHA1.HashData(Encoding.UTF8.GetBytes(toSign));
        var shortSig = Convert.ToBase64String(digest)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=')[..8];

        var url = $"{DeliveryHost}/{_opts.CloudName}/image/upload/s--{shortSig}--/{key}";
        return Task.FromResult(url);
    }

    public async Task DeleteAsync(string key, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var timestamp = time.GetUtcNow().ToUnixTimeSeconds();
        var signedParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["public_id"] = key,
            ["timestamp"] = timestamp.ToString(),
        };
        var signature = Sign(signedParams);

        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["public_id"] = key,
            ["timestamp"] = timestamp.ToString(),
            ["api_key"] = _opts.ApiKey,
            ["signature"] = signature,
        });

        var uri = $"{_opts.ApiBaseUrl}/v1_1/{_opts.CloudName}/image/destroy";
        using var response = await http.PostAsync(uri, body, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<MediaMetadata?> GetMetadataAsync(string key, MediaKind kind, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var resource = kind == MediaKind.Video ? "video" : "image";
        var uri = $"{_opts.ApiBaseUrl}/v1_1/{_opts.CloudName}/resources/{resource}/upload/{Uri.EscapeDataString(key)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_opts.ApiKey}:{_opts.ApiSecret}")));

        using var response = await http.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        int width = root.TryGetProperty("width", out var w) ? w.GetInt32() : 0;
        int height = root.TryGetProperty("height", out var h) ? h.GetInt32() : 0;
        int? duration = root.TryGetProperty("duration", out var d) && d.ValueKind != JsonValueKind.Null
            ? (int)Math.Round(d.GetDouble())
            : null;

        return new MediaMetadata(width, height, duration);
    }

    private string Sign(SortedDictionary<string, string> parameters)
    {
        var joined = string.Join('&', parameters.Select(kv => $"{kv.Key}={kv.Value}"));
        var digest = SHA1.HashData(Encoding.UTF8.GetBytes(joined + _opts.ApiSecret));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }

    private static string ResourceKindFor(string contentType) =>
        contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ? "video" : "image";
}
