using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;

namespace Xpeak.Api.Auth.Google;

/// <summary>GET /auth/google — kicks off the Google OAuth challenge.</summary>
public static class StartGoogleEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("/google", Handle).AllowAnonymous();

    private static IResult Handle() =>
        Results.Challenge(
            new AuthenticationProperties { RedirectUri = "/auth/google/callback" },
            [GoogleDefaults.AuthenticationScheme]);
}
