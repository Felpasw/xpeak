using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xpeak.Api.Challenges.Services;

namespace Xpeak.Api.Challenges.Endpoints;

/// <summary>POST /challenges/{id}/end — owner closes the challenge
/// before <c>ends_at</c>. Flips state to Completed and deletes all
/// share-link invitations in one transaction. 404 for anyone who
/// isn't the owner.</summary>
public static class EndChallengeEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/challenges/{id:guid}/end", Handle)
            .RequireAuthorization()
            .WithTags("Challenges")
            .WithSummary("Owner-only early close for a challenge.");

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

        try
        {
            var ok = await service.EndAsync(id, userId, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        }
        catch (ChallengeAlreadyEndedException)
        {
            return Results.Conflict();
        }
    }
}
