namespace Xpeak.Api.Media.Dto;

public sealed record ConfirmMediaResponse(IReadOnlyList<CheckInMediaResponse> Media);

public sealed record CheckInMediaResponse(
    Guid Id,
    Guid CheckInId,
    string Kind,
    string StorageKey,
    int? Width,
    int? Height,
    int? DurationSeconds,
    int Position,
    DateTimeOffset CreatedAt);
