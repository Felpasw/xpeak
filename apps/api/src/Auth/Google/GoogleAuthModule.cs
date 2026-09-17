using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;

namespace Xpeak.Api.Auth.Google;

/// <summary>
/// Registers the Google OAuth authentication method: a short-lived
/// cookie scheme used to bridge the redirect handoff, the Google
/// handler itself, the account linker, and the two OAuth endpoints.
/// </summary>
public static class GoogleAuthModule
{
    /// <summary>
    /// Cookie scheme used only to carry the Google identity between
    /// the challenge redirect and the callback endpoint. Never issued
    /// to the client as a session cookie.
    /// </summary>
    public const string TempCookieScheme = "GoogleTempCookie";

    public static IServiceCollection AddGoogleAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GoogleOptions>(configuration.GetSection(GoogleOptions.SectionName));
        services.AddScoped<GoogleAccountLinker>();

        services.AddAuthentication()
            .AddCookie(TempCookieScheme, options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                options.Cookie.Name = ".Xpeak.GoogleTemp";
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.HttpOnly = true;
            })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = configuration["Google:ClientId"] ?? "unset";
                options.ClientSecret = configuration["Google:ClientSecret"] ?? "unset";
                options.CallbackPath = "/signin-google";
                options.SignInScheme = TempCookieScheme;
                options.SaveTokens = true;
                options.ClaimActions.MapJsonKey("urn:google:picture", "picture", "url");
            });

        return services;
    }

    public static IEndpointRouteBuilder UseGoogleAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth · Google");
        StartGoogleEndpoint.Map(group);
        GoogleCallbackEndpoint.Map(group);
        return app;
    }
}
