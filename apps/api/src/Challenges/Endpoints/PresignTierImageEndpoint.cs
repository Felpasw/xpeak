using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xpeak.Api.Challenges.Dto;
using Xpeak.Api.Media;
using Xpeak.Api.Media.Dto;

namespace Xpeak.Api.Challenges.Endpoints;

/// <summary>POST /challenges/tier-images/presign — issues a
/// Cloudinary presigned PUT URL plus the storage_key the mobile
/// should echo back as <c>imageStorageKey</c> on the tier payload
/// of <c>POST /challenges</c>. The storage_key namespace is
/// <c>challenges/tier-images/{userId}/{guid}</c>; the caller owns
/// the slot until it's consumed by a tier insert.</summary>
public static class PresignTierImageEndpoint
{
    private const int PresignTtlSeconds = 600;
    // Cloudinary transcodes anyway; this just picks image resource type.
    private const string PhotoMime = "image/jpeg";
    private const long PresignPlaceholderByteSize = 1;

    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/challenges/tier-images/presign", Handle)
            .RequireAuthorization()
            .WithTags("Challenges")
            .WithSummary("Issues a signed upload URL for a tier image.");

    private static async Task<IResult> Handle(
        ClaimsPrincipal principal,
        IMediaStorage storage,
        CancellationToken ct)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return Results.Unauthorized();
        }

        var storageKey = $"challenges/tier-images/{userId:N}/{Guid.NewGuid():N}";
        var upload = await storage.PresignedPutUrlAsync(
            storageKey,
            new PresignOptions(PhotoMime, PresignPlaceholderByteSize, TimeSpan.FromSeconds(PresignTtlSeconds)),
            ct);

        return Results.Ok(new PresignTierImageResponse(
            storageKey,
            upload.Url,
            (int)upload.ExpiresIn.TotalSeconds));
    }
}
