using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xpeak.Api.Infrastructure.Validation;
using Xpeak.Api.Media.Dto;
using Xpeak.Api.Media.Services;

namespace Xpeak.Api.Media.Endpoints;

/// <summary>POST /check_ins/{checkInId}/media/presign — returns
/// signed upload URLs so the client can PUT bytes straight to the
/// storage backend.</summary>
public static class PresignMediaEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/check_ins/{checkInId:guid}/media/presign", Handle)
            .RequireAuthorization()
            .ValidateBody<PresignMediaRequest>()
            .WithTags("Media")
            .WithSummary("Issues signed upload URLs for a check-in's media items.");

    private static async Task<IResult> Handle(
        Guid checkInId,
        PresignMediaRequest request,
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
            var response = await service.RequestPresignsAsync(userId, checkInId, request, ct);
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
    }
}
