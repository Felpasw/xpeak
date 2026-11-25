using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentValidation;
using Xpeak.Api.Social.Dto;
using Xpeak.Api.Social.Repositories;
using Xpeak.Api.Social.Services;

namespace Xpeak.Api.Social.Endpoints;

/// <summary>POST /check_ins/{id}/reactions — idempotent toggle:
/// same emoji from same user hard-deletes, different emoji adds a
/// new row. Response carries the updated aggregate so the client
/// does not have to follow up with a GET.</summary>
public static class ToggleReactionEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/check_ins/{id:guid}/reactions", Handle)
            .RequireAuthorization()
            .WithTags("Social")
            .WithSummary("Toggle an emoji reaction on a check-in.");

    private static async Task<IResult> Handle(
        Guid id,
        ToggleReactionRequest request,
        ClaimsPrincipal principal,
        IValidator<ToggleReactionRequest> validator,
        ICheckInVisibility visibility,
        ICheckInReactionRepository reactions,
        CancellationToken ct)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return Results.Unauthorized();
        }

        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Results.UnprocessableEntity(new
            {
                errors = validation.Errors.Select(e => new { e.PropertyName, code = e.ErrorCode }),
            });
        }

        if (!await visibility.CanSeeAsync(userId, id, ct))
        {
            return Results.NotFound();
        }

        // Phase 13 will subscribe to reaction-added / reaction-removed
        // events once the activity timeline ships. Until then the
        // toggle call is self-contained — no event bus to publish on.
        await reactions.ToggleAsync(id, userId, request.Emoji, ct);

        var aggregate = await reactions.GetAggregateAsync(id, userId, ct);
        return Results.Ok(new ReactionsResponse(aggregate.Counts, aggregate.Mine));
    }
}
