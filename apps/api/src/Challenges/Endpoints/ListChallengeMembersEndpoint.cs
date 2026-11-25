using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Challenges.Dto;
using Xpeak.Api.Challenges.Services;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Users;

namespace Xpeak.Api.Challenges.Endpoints;

/// <summary>GET /challenges/{id}/members — ranked list of every
/// member of the challenge, ordered by their challenge-scoped
/// progress (descending) with <c>UserId</c> as a stable tiebreak.
/// 404 when the caller is not a member of the challenge (same
/// no-existence-leak rule the progress endpoint follows).</summary>
public static class ListChallengeMembersEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/challenges/{id:guid}/members", Handle)
            .RequireAuthorization()
            .WithTags("Challenges")
            .WithSummary("Ranked member list for a single challenge.");

    private static async Task<IResult> Handle(
        Guid id,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken ct)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is null || !Guid.TryParse(sub, out var callerId))
        {
            return Results.Unauthorized();
        }

        var callerIsMember = await db.ChallengeMemberships
            .AsNoTracking()
            .AnyAsync(m => m.ChallengeId == id && m.UserId == callerId, ct);
        if (!callerIsMember)
        {
            return Results.NotFound();
        }

        var memberships = await db.ChallengeMemberships
            .AsNoTracking()
            .Where(m => m.ChallengeId == id)
            .Join(
                db.Users.AsNoTracking(),
                m => m.UserId,
                u => u.Id,
                (membership, user) => new { Membership = membership, User = user })
            .ToListAsync(ct);

        var tiers = await db.ChallengeTiers
            .AsNoTracking()
            .Where(t => t.ChallengeId == id)
            .OrderBy(t => t.MinLevel)
            .ToListAsync(ct);

        var ranked = memberships
            .OrderByDescending(x => x.Membership.Progress.Value)
            .ThenBy(x => x.Membership.UserId)
            .Select(x =>
            {
                var tier = ChallengeTierResolver.ResolveCurrent(
                    tiers,
                    x.Membership.Progress.Value);
                return new ChallengeMemberRankingResponse(
                    PublicUserResponse.From(x.User),
                    MembershipProgressResponse.From(x.Membership.Progress),
                    tier is null
                        ? null
                        : new ChallengeMemberTierResponse(tier.Id, tier.MinLevel, tier.Title));
            })
            .ToList();

        return Results.Ok(new ListChallengeMembersResponse(ranked));
    }
}
