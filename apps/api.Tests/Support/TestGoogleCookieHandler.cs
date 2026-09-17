using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Xpeak.Api.Tests.Support;

/// <summary>
/// Test double for the Google temp cookie scheme. Reads a canned identity from
/// per-request headers instead of a real Google-issued cookie. Also implements
/// sign-in/out so the callback endpoint's <c>SignOutAsync(TempCookieScheme)</c>
/// doesn't blow up.
/// </summary>
public sealed class TestGoogleCookieHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : SignInAuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string ForceFailHeader = "X-Test-Google-Fail";
    public const string UidHeader = "X-Test-Google-Uid";
    public const string EmailHeader = "X-Test-Google-Email";
    public const string AvatarHeader = "X-Test-Google-Avatar";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.ContainsKey(ForceFailHeader))
        {
            return Task.FromResult(AuthenticateResult.Fail("stubbed google failure"));
        }

        if (!Request.Headers.TryGetValue(UidHeader, out var uid) || string.IsNullOrEmpty(uid.ToString()))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, uid.ToString()),
            new(ClaimTypes.Email, Request.Headers[EmailHeader].ToString()),
        };

        var avatar = Request.Headers[AvatarHeader].ToString();
        if (!string.IsNullOrEmpty(avatar))
        {
            claims.Add(new Claim("urn:google:picture", avatar));
        }

        var identity = new ClaimsIdentity(claims, "TestGoogle");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties) =>
        Task.CompletedTask;

    protected override Task HandleSignOutAsync(AuthenticationProperties? properties) =>
        Task.CompletedTask;
}
