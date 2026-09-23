using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Xpeak.Api.Auth.Core;

namespace Xpeak.Api.Auth.Google;

/// <summary>
/// GET /auth/google/callback — reads Google's cookie handoff, links or
/// creates the local account, then redirects to the mobile deep link
/// carrying the freshly-issued JWT.
/// </summary>
public static class GoogleCallbackEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("/google/callback", Handle).AllowAnonymous();

    private static async Task<IResult> Handle(
        HttpContext ctx,
        GoogleAccountLinker linker,
        JwtTokenIssuer tokenIssuer,
        IConfiguration config,
        CancellationToken ct)
    {
        var callbackBase = config["Auth:CallbackUri"] ?? "xpeak://auth/callback";

        var authResult = await ctx.AuthenticateAsync(GoogleAuthModule.TempCookieScheme);
        if (!authResult.Succeeded || authResult.Principal is null)
        {
            return Results.Redirect($"{callbackBase}?error=google_failed");
        }

        var googleUid = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = authResult.Principal.FindFirstValue(ClaimTypes.Email);
        var avatar = authResult.Principal.FindFirstValue("urn:google:picture");

        if (googleUid is null || string.IsNullOrEmpty(email))
        {
            return Results.Redirect($"{callbackBase}?error=missing_google_claims");
        }

        var identity = new GoogleIdentity(
            GoogleUid: googleUid,
            Email: email,
            AvatarUrl: avatar,
            UsernameSeed: email.Split('@')[0]);

        var user = await linker.FindOrCreateAsync(identity, ct);
        await ctx.SignOutAsync(GoogleAuthModule.TempCookieScheme);

        var token = tokenIssuer.Issue(user);
        return Results.Redirect($"{callbackBase}?token={Uri.EscapeDataString(token.Value)}");
    }
}
