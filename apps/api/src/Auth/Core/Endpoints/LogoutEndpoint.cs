using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Xpeak.Api.Auth.Core;

/// <summary>POST /auth/logout — revokes the current bearer's jti.</summary>
public static class LogoutEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("/logout", Handle).RequireAuthorization();

    private static async Task<IResult> Handle(
        ClaimsPrincipal principal,
        RevokedTokenRepository repo,
        CancellationToken ct)
    {
        var jti = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var expClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Exp);

        if (jti is null || sub is null || expClaim is null
            || !Guid.TryParse(sub, out var userId)
            || !long.TryParse(expClaim, out var exp))
        {
            return Results.Unauthorized();
        }

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp);
        await repo.AddAsync(jti, userId, expiresAt, ct);
        return Results.NoContent();
    }
}
