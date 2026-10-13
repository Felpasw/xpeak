using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xpeak.Api.Challenges.Services;

namespace Xpeak.Api.Challenges.Endpoints;

/// <summary>DELETE /challenges/{id} — hard-deletes the challenge
/// (cascades memberships + categories). Owner only; 404 for
/// anyone else or on missing row (no existence leak).</summary>
public static class DeleteChallengeEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/challenges/{id:guid}", Handle)
            .RequireAuthorization()
            .WithTags("Challenges")
            .WithSummary("Deletes a challenge owned by the caller.");

    private static async Task<IResult> Handle(
        Guid id,
        ClaimsPrincipal principal,
        IChallengeService service,
        CancellationToken ct)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return Results.Unauthorized();
        }

        var deleted = await service.DeleteAsync(id, userId, ct);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
