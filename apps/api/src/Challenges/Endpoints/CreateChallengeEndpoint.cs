using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xpeak.Api.Challenges.Dto;
using Xpeak.Api.Challenges.Services;
using Xpeak.Api.Infrastructure.Validation;

namespace Xpeak.Api.Challenges.Endpoints;

/// <summary>POST /challenges — creates a challenge and auto-joins
/// the caller as owner.</summary>
public static class CreateChallengeEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/challenges", Handle)
            .RequireAuthorization()
            .ValidateBody<CreateChallengeRequest>()
            .WithTags("Challenges")
            .WithSummary("Creates a challenge with the caller as owner.");

    private static async Task<IResult> Handle(
        CreateChallengeRequest request,
        ClaimsPrincipal principal,
        IChallengeService service,
        CancellationToken ct)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return Results.Unauthorized();
        }

        var tiers = request.Tiers?
            .Select(t => new CreateChallengeTierInput(t.MinLevel, t.Title))
            .ToList();

        var result = await service.CreateAsync(
            userId,
            new CreateChallengeInput(
                request.Title,
                request.Description,
                request.IconPublicId,
                request.Visibility,
                request.TargetLevel,
                request.XpMultiplier,
                request.StartsAt,
                request.EndsAt,
                request.MemberCap,
                tiers),
            ct);

        var response = new CreateChallengeResponse(
            ChallengeResponse.From(result.Challenge),
            MembershipResponse.From(result.OwnerMembership),
            ChallengeInvitationResponse.From(result.ShareLink));

        return Results.Created($"/challenges/{result.Challenge.Id}", response);
    }
}
