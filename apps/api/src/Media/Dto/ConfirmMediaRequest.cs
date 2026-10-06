namespace Xpeak.Api.Media.Dto;

/// <summary>
/// Confirm payload: the client only tells us which <c>storage_key</c>
/// it just uploaded and where to place it in the carousel. All the
/// numeric metadata (width, height, duration) is fetched from the
/// storage backend, not the client — so a forged payload cannot
/// falsify aspect ratio or duration.
/// </summary>
public sealed record ConfirmMediaRequest(IReadOnlyList<ConfirmMediaItem> Items);

public sealed record ConfirmMediaItem(string StorageKey, string Kind, int Position);
