using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xpeak.Api.Media.Services;

namespace Xpeak.Api.Media.Endpoints;

/// <summary>GET /check_ins/{checkInId}/media/{mediaId}/url — returns a
/// short-lived signed delivery URL. The URL is minted on demand so it
/// expires quickly and cannot leak indefinitely from client logs.</summary>
public static class GetMediaUrlEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/check_ins/{checkInId:guid}/media/{mediaId:guid}/url", Handle)
            .RequireAuthorization()
            .WithTags("Media")
            .WithSummary("Returns a fresh signed URL for downloading a specific media asset.");

    private static async Task<IResult> Handle(
        Guid checkInId,
        Guid mediaId,
        ClaimsPrincipal principal,
        IMediaService service,
        CancellationToken ct)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var response = await service.GetSignedUrlAsync(userId, checkInId, mediaId, ct);
            return Results.Ok(response);
        }
        catch (CheckInNotFoundException)
        {
            return Results.NotFound();
        }
        catch (CheckInNotOwnedException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (MediaNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
