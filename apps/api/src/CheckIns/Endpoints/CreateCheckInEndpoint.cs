using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Services;
using Xpeak.Api.Infrastructure.Validation;

namespace Xpeak.Api.CheckIns.Endpoints;

/// <summary>POST /check_ins — logs a check-in and returns updated XP + streak.</summary>
public static class CreateCheckInEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/check_ins", Handle)
            .RequireAuthorization()
            .ValidateBody<CreateCheckInRequest>()
            .WithTags("Check-ins")
            .WithSummary("Creates a check-in and returns the updated user + streak.");

    private static async Task<IResult> Handle(
        CreateCheckInRequest request,
        ClaimsPrincipal principal,
        ICheckInService service,
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
                new CreateCheckInInput(request.CategoryId, request.DurationMinutes, request.Notes),
                ct);

            var response = new CreateCheckInResponse(
                CheckInResponse.From(result.CheckIn),
                new UserProgressionResponse(
                    result.User.Id,
                    result.User.Xp,
                    result.User.Level,
                    result.LevelUp.LeveledUp,
                    result.LevelUp.LevelsGained),
                new StreakResponse(
                    result.Streak.Current,
                    result.Streak.Longest,
                    result.Streak.Unit.ToString().ToLowerInvariant()));

            return Results.Created($"/check_ins/{result.CheckIn.Id}", response);
        }
        catch (CategoryNotFoundException)
        {
            return Results.NotFound(new { error = "category_not_found" });
        }
        catch (NotAGroupMemberException)
        {
            return Results.Json(
                new { error = "not_a_group_member" },
                statusCode: StatusCodes.Status403Forbidden);
        }
    }
}
