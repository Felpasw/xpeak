using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Xpeak.Api.Users;

namespace Xpeak.Api.Auth.Core;

/// <summary>GET /me — returns the currently authenticated user.</summary>
public static class MeEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/me", Handle)
            .RequireAuthorization()
            .WithTags("Me")
            .WithSummary("Returns the current authenticated user.");

    private static async Task<IResult> Handle(
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new AuthResponse(user.ToResponse()));
    }
}
