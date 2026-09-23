using Microsoft.AspNetCore.Identity;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.Groups.Services;
using Xpeak.Api.Users;

namespace Xpeak.Api.Auth.Password;

/// <summary>POST /auth/register — email + password sign-up.</summary>
public static class RegisterEndpoint
{
    public const string RateLimitPolicy = "auth-register";

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("/register", Handle).RequireRateLimiting(RateLimitPolicy);

    private static async Task<IResult> Handle(
        RegisterRequest request,
        UserManager<AppUser> userManager,
        JwtTokenIssuer tokenIssuer,
        IGroupService groups,
        HttpContext ctx,
        TimeProvider time,
        CancellationToken ct)
    {
        var user = new AppUser
        {
            UserName = request.Username?.Trim().ToLowerInvariant(),
            Email = request.Email?.Trim().ToLowerInvariant(),
            EmailConfirmed = false,
            CreatedAt = time.GetUtcNow(),
        };

        var result = await userManager.CreateAsync(user, request.Password ?? string.Empty);
        if (!result.Succeeded)
        {
            return Results.UnprocessableEntity(new
            {
                errors = result.Errors.Select(e => new { e.Code, e.Description }),
            });
        }

        await groups.AddUserToGlobalAsync(user.Id, ct);

        var token = tokenIssuer.Issue(user);
        ctx.Response.Headers.Authorization = $"Bearer {token.Value}";
        return Results.Created($"/users/{user.Id}", new AuthResponse(user.ToResponse()));
    }
}
