namespace Xpeak.Api.Media.Dto;

/// <summary>
/// A ready-to-use signed upload target. The client PUTs the file bytes
/// straight to <paramref name="Url"/>; the backend never sees them.
/// </summary>
public sealed record PresignedUpload(string Url, TimeSpan ExpiresIn);
