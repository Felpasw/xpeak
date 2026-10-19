using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xpeak.Api.Challenges.Dto;
using Xpeak.Api.Challenges.Services;
using Xpeak.Api.Infrastructure.Validation;

namespace Xpeak.Api.Challenges.Endpoints;

/// <summary>POST /challenges/join — joins the caller into a
/// challenge via a share-link invitation id. The invitation id is
/// the public share token embedded in the deep-link URL.</summary>
public static class JoinChallengeEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/challenges/join", Handle)
            .RequireAuthorization()
            .ValidateBody<JoinChallengeRequest>()
            .WithTags("Challenges")
            .WithSummary("Joins the caller into a challenge via a share-link invitation.");

    private static async Task<IResult> Handle(
        JoinChallengeRequest request,
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
            var result = await service.JoinWithInvitationAsync(request.InvitationId, userId, ct);
            var response = new JoinChallengeResponse(
                ChallengeResponse.From(result.Challenge),
                MembershipResponse.From(result.Membership));
            return Results.Ok(response);
        }
        catch (InvitationNotFoundException)
        {
            return Results.NotFound();
        }
        catch (InvitationNotActiveException)
        {
            return Results.Conflict();
        }
        catch (ChallengeNotActiveException)
        {
            return Results.Conflict();
        }
        catch (AlreadyMemberException)
        {
            return Results.Conflict();
        }
        catch (MemberCapReachedException)
        {
            return Results.Conflict();
        }
    }
}
