using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xpeak.Api.Challenges.Dto;
using Xpeak.Api.Challenges.Entities;
using Xpeak.Api.Challenges.Services;

namespace Xpeak.Api.Challenges.Endpoints;

/// <summary>GET /challenges — lists the caller's memberships,
/// optionally filtered by state.</summary>
public static class ListChallengesEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/challenges", Handle)
            .RequireAuthorization()
            .WithTags("Challenges")
            .WithSummary("Lists the caller's challenges, newest first.");

    private static async Task<IResult> Handle(
        ClaimsPrincipal principal,
        IChallengeService service,
        string? state,
        CancellationToken ct)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return Results.Unauthorized();
        }

        ChallengeState? stateFilter = null;
        if (!string.IsNullOrEmpty(state))
        {
            if (!Enum.TryParse<ChallengeState>(state, ignoreCase: true, out var parsed))
            {
                return Results.BadRequest(new { error = "invalid_state_filter" });
            }
            stateFilter = parsed;
        }

        var details = await service.ListForUserAsync(userId, stateFilter, ct);
        var response = new ListChallengesResponse(
            details.Select(d => ChallengeResponse.From(d.Challenge, d.CategoryIds)).ToList());

        return Results.Ok(response);
    }
}
