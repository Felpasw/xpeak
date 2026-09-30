namespace Xpeak.Api.Media.Dto;

public sealed record PresignMediaRequest(IReadOnlyList<PresignMediaItem> Items);

public sealed record PresignMediaItem(string Kind);
