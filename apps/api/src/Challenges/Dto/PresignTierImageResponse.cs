namespace Xpeak.Api.Challenges.Dto;

/// <summary>Payload of <c>POST /challenges/tier-images/presign</c>.
/// The mobile uses <see cref="UploadUrl"/> to PUT the file bytes
/// to Cloudinary (with <see cref="FormFields"/> as multipart form
/// data when the provider is Cloudinary), then sends
/// <see cref="StorageKey"/> back in the tier payload of
/// <c>POST /challenges</c>.</summary>
public sealed record PresignTierImageResponse(
    string StorageKey,
    string UploadUrl,
    int ExpiresInSeconds);
