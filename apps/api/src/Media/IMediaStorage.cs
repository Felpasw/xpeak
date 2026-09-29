using Xpeak.Api.Media.Dto;

namespace Xpeak.Api.Media;

/// <summary>
/// Storage backend for user-generated media (check-in photos and videos).
/// The API only hands out signed URLs; bytes never transit the app.
/// </summary>
public interface IMediaStorage
{
    Task<PresignedUpload> PresignedPutUrlAsync(
        string key,
        PresignOptions options,
        CancellationToken ct);

    Task<string> PresignedGetUrlAsync(
        string key,
        TimeSpan ttl,
        CancellationToken ct);

    Task DeleteAsync(string key, CancellationToken ct);
}
