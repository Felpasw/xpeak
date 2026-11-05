using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Infrastructure;

namespace Xpeak.Api.Users;

/// <summary>GET /users/{username} — public profile card of any
/// user. Case-insensitive lookup against
/// <c>NormalizedUserName</c>; 404 when there is no match.
/// Authentication required so the endpoint stays inside the app's
/// signed-in surface (not the public internet).</summary>
public static class GetUserByUsernameEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/users/{username}", Handle)
            .RequireAuthorization()
            .WithTags("Users")
            .WithSummary("Returns the public profile card of a user by username.");

    private static async Task<IResult> Handle(
        string username,
        AppDbContext db,
        CancellationToken ct)
    {
        var normalized = username.ToUpperInvariant();
        var user = await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.NormalizedUserName == normalized, ct);
        if (user is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(PublicUserResponse.From(user));
    }
}
