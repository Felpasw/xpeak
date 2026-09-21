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
                options.ClientId = OrPlaceholder(configuration["Google:ClientId"]);
                options.ClientSecret = OrPlaceholder(configuration["Google:ClientSecret"]);
                options.CallbackPath = "/signin-google";
                options.SignInScheme = TempCookieScheme;
                options.SaveTokens = true;
                options.ClaimActions.MapJsonKey("urn:google:picture", "picture", "url");
            });

        return services;
    }

    // OAuthOptions.Validate() rejects null AND empty strings, so an unset env
    // var (missing OR blank) needs a non-empty placeholder. The Google flow
    // still fails at OAuth exchange with real credentials, but the app boots
    // and unrelated endpoints keep working.
    private static string OrPlaceholder(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "unset" : value;

    public static IEndpointRouteBuilder UseGoogleAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth · Google");
        StartGoogleEndpoint.Map(group);
        GoogleCallbackEndpoint.Map(group);
        return app;
    }
}
