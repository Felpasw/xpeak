namespace Xpeak.Api.Media.Dto;

public sealed record PresignMediaResponse(IReadOnlyList<PresignMediaSlot> Presigned);

public sealed record PresignMediaSlot(string StorageKey, string Url, int ExpiresIn);
