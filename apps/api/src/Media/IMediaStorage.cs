using Xpeak.Api.Media.Dto;
using Xpeak.Api.Media.Entities;

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

    /// <summary>
    /// Looks up the authoritative metadata for a stored asset. Returns
    /// null when the backend has no record of it — the confirm flow
    /// uses this both as an existence check and as the source of
    /// truth for width, height and duration.
    /// </summary>
    Task<MediaMetadata?> GetMetadataAsync(
        string key,
        MediaKind kind,
        CancellationToken ct);
}
