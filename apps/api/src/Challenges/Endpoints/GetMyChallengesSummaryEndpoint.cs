using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Challenges.Dto;
using Xpeak.Api.Challenges.Entities;
using Xpeak.Api.Challenges.Services;
using Xpeak.Api.Infrastructure;

namespace Xpeak.Api.Challenges.Endpoints;

/// <summary>GET /challenges/summary — compact list of the caller's
/// active challenges with inline per-membership progress. Shape is
/// optimized for the home dashboard; the broader
/// <c>/challenges</c> listing is untouched.</summary>
public static class GetMyChallengesSummaryEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/challenges/summary", Handle)
            .RequireAuthorization()
            .WithTags("Challenges")
            .WithSummary("Compact active-challenge summary for the home dashboard.");

    private static async Task<IResult> Handle(
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

        var rows = await db.ChallengeMemberships
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Join(db.Challenges.AsNoTracking().Where(c => c.State == ChallengeState.Active),
                m => m.ChallengeId,
                c => c.Id,
                (m, c) => new { Membership = m, Challenge = c })
            .OrderByDescending(x => x.Challenge.CreatedAt)
            .ToListAsync(ct);

        var summaries = new List<ChallengeSummaryResponse>(rows.Count);
        foreach (var row in rows)
        {
            var progress = await calculator.ComputeAsync(row.Membership.Id, ct);
            summaries.Add(new ChallengeSummaryResponse(
                row.Challenge.Id,
                row.Challenge.Title,
                row.Challenge.State,
                MembershipProgressResponse.From(progress)));
        }

        return Results.Ok(new ListChallengesSummaryResponse(summaries));
    }
}
