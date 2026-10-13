using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Challenges.Dto;
using Xpeak.Api.Challenges.Services;
using Xpeak.Api.Infrastructure;

namespace Xpeak.Api.Challenges.Endpoints;

/// <summary>GET /challenges/{id}/progress — returns the caller's
/// progress on a challenge. 404 when the caller is not a member
/// (no existence leak, no progress for non-members).</summary>
public static class GetChallengeProgressEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/challenges/{id:guid}/progress", Handle)
            .RequireAuthorization()
            .WithTags("Challenges")
            .WithSummary("Reads the caller's progress on a challenge.");

    private static async Task<IResult> Handle(
        Guid id,
        ClaimsPrincipal principal,
        IProgressCalculator calculator,
        AppDbContext db,
        CancellationToken ct)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return Results.Unauthorized();
        }

        var membership = await db.ChallengeMemberships
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.ChallengeId == id && m.UserId == userId, ct);
        if (membership is null)
        {
            return Results.NotFound();
        }

        var progress = await calculator.ComputeAsync(membership.Id, ct);
        return Results.Ok(new ChallengeProgressResponse(
            MembershipProgressResponse.From(progress)));
    }
}
