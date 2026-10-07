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

        try
        {
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
                    request.CategoryIds),
                ct);

            var response = new CreateChallengeResponse(
                ChallengeResponse.From(
                    result.Challenge,
                    result.Categories.Select(c => c.CategoryId).ToList()),
                MembershipResponse.From(result.OwnerMembership));

            return Results.Created($"/challenges/{result.Challenge.Id}", response);
        }
        catch (CategoryNotFoundException ex)
        {
            return Results.NotFound(new { error = "category_not_found", categoryId = ex.CategoryId });
        }
        catch (NoCategoriesException)
        {
            // Validator should catch empty list earlier; this is a belt
            // against programmatic callers that bypass validation.
            return Results.UnprocessableEntity(new { error = "no_categories" });
        }
    }
}
