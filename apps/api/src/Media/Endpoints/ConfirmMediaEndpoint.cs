using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xpeak.Api.Infrastructure.Validation;
using Xpeak.Api.Media.Dto;
using Xpeak.Api.Media.Services;

namespace Xpeak.Api.Media.Endpoints;

/// <summary>POST /check_ins/{checkInId}/media — the client confirms
/// completed uploads by echoing each storage_key. The backend then
/// pulls the authoritative metadata (width, height, duration) from
/// the storage backend and creates the check_in_media rows.</summary>
public static class ConfirmMediaEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/check_ins/{checkInId:guid}/media", Handle)
            .RequireAuthorization()
            .ValidateBody<ConfirmMediaRequest>()
            .WithTags("Media")
            .WithSummary("Confirms uploaded media and persists the rows with backend-verified metadata.");

    private static async Task<IResult> Handle(
        Guid checkInId,
        ConfirmMediaRequest request,
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
            var response = await service.ConfirmMediaAsync(userId, checkInId, request, ct);
            return Results.Created($"/check_ins/{checkInId}/media", response);
        }
        catch (CheckInNotFoundException)
        {
            return Results.NotFound();
        }
        catch (CheckInNotOwnedException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (UnknownStorageKeyException ex)
        {
            return Results.UnprocessableEntity(new
            {
                errors = new[] { new { field = "storage_key", message = ex.Message } },
            });
        }
        catch (StorageKeyCheckInMismatchException ex)
        {
            return Results.UnprocessableEntity(new
            {
                errors = new[] { new { field = "storage_key", message = ex.Message } },
            });
        }
    }
}
