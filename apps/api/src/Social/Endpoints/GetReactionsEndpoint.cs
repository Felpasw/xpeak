using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xpeak.Api.Social.Dto;
using Xpeak.Api.Social.Repositories;
using Xpeak.Api.Social.Services;

namespace Xpeak.Api.Social.Endpoints;

/// <summary>GET /check_ins/{id}/reactions — aggregate counts + the
/// caller's own emojis. <c>?include=users</c> adds a grouped user
/// list capped at 20 per emoji (oldest-first).</summary>
public static class GetReactionsEndpoint
{
    private const int UsersPerEmojiCap = 20;

    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/check_ins/{id:guid}/reactions", Handle)
            .RequireAuthorization()
            .WithTags("Social")
            .WithSummary("Read the reaction aggregate for a check-in.");

    private static async Task<IResult> Handle(
        Guid id,
        string? include,
        ClaimsPrincipal principal,
        ICheckInVisibility visibility,
        ICheckInReactionRepository reactions,
        CancellationToken ct)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return Results.Unauthorized();
        }

        if (!await visibility.CanSeeAsync(userId, id, ct))
        {
            return Results.NotFound();
        }

        var aggregate = await reactions.GetAggregateAsync(id, userId, ct);

        if (!string.Equals(include, "users", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Ok(new ReactionsResponse(aggregate.Counts, aggregate.Mine));
        }

        var users = await reactions.GetUsersAsync(id, UsersPerEmojiCap, ct);
        var usersDto = users.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<ReactionUserDto>)kv.Value
                .Select(u => new ReactionUserDto(u.Id, u.Username, u.AvatarUrl))
                .ToList());

        return Results.Ok(new ReactionsWithUsersResponse(
            aggregate.Counts, aggregate.Mine, usersDto));
    }
}
