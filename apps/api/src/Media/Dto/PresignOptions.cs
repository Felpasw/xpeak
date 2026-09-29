namespace Xpeak.Api.Media.Dto;

/// <summary>
/// Metadata forwarded to the storage backend when asking for a presigned
/// upload URL. <paramref name="Ttl"/> is how long the URL stays valid; the
/// backend is free to clamp it to a lower ceiling.
/// </summary>
public sealed record PresignOptions(string ContentType, long ByteSize, TimeSpan Ttl);
