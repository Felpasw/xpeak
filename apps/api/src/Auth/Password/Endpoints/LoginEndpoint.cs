using Microsoft.AspNetCore.Identity;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.Users;

namespace Xpeak.Api.Auth.Password;

/// <summary>POST /auth/login — email/username + password.</summary>
public static class LoginEndpoint
{
    public const string RateLimitPolicy = "auth-login-ip";

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("/login", Handle).RequireRateLimiting(RateLimitPolicy);

    private static async Task<IResult> Handle(
        LoginRequest request,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        FailedLoginCounter failedLogin,
        JwtTokenIssuer tokenIssuer,
        HttpContext ctx,
        CancellationToken ct)
    {
        var identifier = request.Identifier?.Trim() ?? string.Empty;
        if (identifier.Length == 0 || string.IsNullOrEmpty(request.Password))
        {
            return Results.UnprocessableEntity(new { error = "invalid_request" });
        }

        if (failedLogin.IsLocked(identifier))
        {
            return Results.StatusCode(StatusCodes.Status429TooManyRequests);
        }

        var user = identifier.Contains('@', StringComparison.Ordinal)
            ? await userManager.FindByEmailAsync(identifier)
            : await userManager.FindByNameAsync(identifier.ToLowerInvariant());

        if (user is null)
        {
            failedLogin.RecordFailure(identifier);
            return Results.Unauthorized();
        }

        var check = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!check.Succeeded)
        {
            failedLogin.RecordFailure(identifier);
            return Results.Unauthorized();
        }

        failedLogin.Clear(identifier);
        var token = tokenIssuer.Issue(user);
        ctx.Response.Headers.Authorization = $"Bearer {token.Value}";
        return Results.Ok(new AuthResponse(user.ToResponse()));
    }
}
