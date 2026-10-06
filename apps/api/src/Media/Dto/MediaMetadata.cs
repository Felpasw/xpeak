namespace Xpeak.Api.Media.Dto;

/// <summary>
/// Authoritative asset metadata as reported by the storage backend.
/// Used to reject client-provided values that don't match what the
/// backend actually has (or has not).
/// </summary>
public sealed record MediaMetadata(int Width, int Height, int? DurationSeconds);
