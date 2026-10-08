using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xpeak.Api.Challenges.Dto;
using Xpeak.Api.Challenges.Services;

namespace Xpeak.Api.Challenges.Endpoints;

/// <summary>GET /challenges/{id} — detail. Returns 404 when the
/// caller is not a member and the challenge is not public (no
/// existence leak).</summary>
public static class GetChallengeEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/challenges/{id:guid}", Handle)
            .RequireAuthorization()
            .WithTags("Challenges")
            .WithSummary("Reads a challenge detail by id.");

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

        var detail = await service.GetForUserAsync(id, userId, ct);
        if (detail is null)
        {
            return Results.NotFound();
        }

        var response = new GetChallengeResponse(
            ChallengeResponse.From(detail.Challenge),
            detail.Membership is null ? null : MembershipResponse.From(detail.Membership));

        return Results.Ok(response);
    }
}
