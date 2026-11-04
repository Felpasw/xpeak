using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Infrastructure.Validation;
using Xpeak.Api.Users;

namespace Xpeak.Api.Auth.Core;

/// <summary>PATCH /me/background — updates the authenticated
/// user's picked background and returns the fresh <c>/me</c>
/// payload. Writes through <see cref="AppDbContext"/> directly so
/// Identity's user validators (reserved-name block, uniqueness,
/// …) do not re-run on an unrelated single-column update. Unknown
/// or blank slugs are rejected by the
/// <see cref="UpdateBackgroundRequestValidator"/> as 422.</summary>
public static class UpdateBackgroundEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPatch("/me/background", Handle)
            .RequireAuthorization()
            .ValidateBody<UpdateBackgroundRequest>()
            .WithTags("Me")
            .WithSummary("Updates the current user's background.");

    private static async Task<IResult> Handle(
        UpdateBackgroundRequest request,
        ClaimsPrincipal principal,
        AppDbContext db)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        BackgroundsExtensions.TryParseWire(request.Background!, out var slug);
        user.Background = slug;
        await db.SaveChangesAsync();

        return Results.Ok(new AuthResponse(user.ToResponse()));
    }
}
