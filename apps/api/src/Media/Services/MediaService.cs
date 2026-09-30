using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Media.Dto;
using Xpeak.Api.Media.Entities;

namespace Xpeak.Api.Media.Services;

public sealed class MediaService(
    AppDbContext db,
    IMediaStorage storage,
    IPendingMediaCache pending,
    TimeProvider time) : IMediaService
{
    private const int PresignTtlSeconds = 600;
    private static readonly TimeSpan PresignTtl = TimeSpan.FromSeconds(PresignTtlSeconds);

    // Photos default to jpeg (Cloudinary transcodes on upload anyway;
    // this mime just picks image vs video resource type on their side).
    // Videos default to mp4 for the same reason.
    private const string PhotoMime = "image/jpeg";
    private const string VideoMime = "video/mp4";
    private const long PresignPlaceholderByteSize = 1;

    public async Task<PresignMediaResponse> RequestPresignsAsync(
        Guid userId,
        Guid checkInId,
        PresignMediaRequest request,
        CancellationToken ct)
    {
        await EnsureOwnershipAsync(userId, checkInId, ct);

        var slots = new List<PresignMediaSlot>(request.Items.Count);
        foreach (var item in request.Items)
        {
            var kind = ParseKind(item.Kind);
            var storageKey = $"checkins/{checkInId:N}/{Guid.NewGuid():N}";
            var mime = kind == MediaKind.Photo ? PhotoMime : VideoMime;

            var upload = await storage.PresignedPutUrlAsync(
                storageKey,
                new PresignOptions(mime, PresignPlaceholderByteSize, PresignTtl),
                ct);

            pending.Register(storageKey, checkInId);
            slots.Add(new PresignMediaSlot(storageKey, upload.Url, (int)upload.ExpiresIn.TotalSeconds));
        }
        return new PresignMediaResponse(slots);
    }

    public async Task<ConfirmMediaResponse> ConfirmMediaAsync(
        Guid userId,
        Guid checkInId,
        ConfirmMediaRequest request,
        CancellationToken ct)
    {
        await EnsureOwnershipAsync(userId, checkInId, ct);

        // Reserve each key against the pending cache *before* hitting the
        // storage backend — this fails fast on forged / expired keys and
        // avoids wasting Cloudinary Admin API calls on invalid input.
        var reservations = new List<Reservation>(request.Items.Count);
        foreach (var item in request.Items)
        {
            var issuedFor = pending.Consume(item.StorageKey)
                ?? throw new UnknownStorageKeyException(item.StorageKey);
            if (issuedFor != checkInId)
            {
                throw new StorageKeyCheckInMismatchException(item.StorageKey, issuedFor, checkInId);
            }
            reservations.Add(new Reservation(item, ParseKind(item.Kind)));
        }

        // Fetch authoritative metadata from the storage backend in parallel.
        // Anything the client sent for width/height/duration is ignored.
        var metadataTasks = reservations
            .Select(r => storage.GetMetadataAsync(r.Item.StorageKey, r.Kind, ct))
            .ToList();
        var metadata = await Task.WhenAll(metadataTasks);

        var now = time.GetUtcNow();
        var rows = new List<CheckInMedia>(reservations.Count);
        for (var i = 0; i < reservations.Count; i++)
        {
            var meta = metadata[i]
                ?? throw new UnknownStorageKeyException(reservations[i].Item.StorageKey);

            rows.Add(new CheckInMedia
            {
                Id = Guid.NewGuid(),
                CheckInId = checkInId,
                Kind = reservations[i].Kind,
                StorageKey = reservations[i].Item.StorageKey,
                Width = meta.Width > 0 ? meta.Width : null,
                Height = meta.Height > 0 ? meta.Height : null,
                DurationSeconds = meta.DurationSeconds,
                Position = reservations[i].Item.Position,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
        db.CheckInMedia.AddRange(rows);
        await db.SaveChangesAsync(ct);

        return new ConfirmMediaResponse(rows.Select(ToResponse).ToList());
    }

    private static CheckInMediaResponse ToResponse(CheckInMedia row) => new(
        row.Id,
        row.CheckInId,
        row.Kind.ToString().ToLowerInvariant(),
        row.StorageKey,
        row.Width,
        row.Height,
        row.DurationSeconds,
        row.Position,
        row.CreatedAt);

    private async Task EnsureOwnershipAsync(Guid userId, Guid checkInId, CancellationToken ct)
    {
        var row = await db.CheckIns
            .AsNoTracking()
            .Where(c => c.Id == checkInId)
            .Select(c => new { c.UserId })
            .SingleOrDefaultAsync(ct);

        if (row is null)
        {
            throw new CheckInNotFoundException(checkInId);
        }
        if (row.UserId != userId)
        {
            throw new CheckInNotOwnedException(checkInId, userId);
        }
    }

    private static MediaKind ParseKind(string kind) =>
        kind.Equals("video", StringComparison.OrdinalIgnoreCase) ? MediaKind.Video : MediaKind.Photo;

    private sealed record Reservation(ConfirmMediaItem Item, MediaKind Kind);
}
