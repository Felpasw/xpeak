using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Xpeak.Api.Auth.Core;

/// <summary>
/// Cross-cutting auth infrastructure shared by every authentication
/// method (password, Google, future passkey, ...): JWT bearer scheme,
/// token issuance, revocation store + background sweeper, the
/// authorization service, and the post-auth endpoints (<c>/me</c>,
/// <c>/auth/logout</c>).
/// </summary>
public static class AuthCoreModule
{
    public static IServiceCollection AddAuthCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<JwtTokenIssuer>();
        services.AddScoped<RevokedTokenRepository>();
        services.AddHostedService<RevokedTokenSweeper>();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>(ConfigureJwtBearer);

        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// Maps <c>/me</c> and <c>/auth/logout</c>. Each authentication
    /// method module maps its own endpoints under <c>/auth</c>.
    /// </summary>
    public static IEndpointRouteBuilder UseAuthCore(this IEndpointRouteBuilder app)
    {
        MeEndpoint.Map(app);

        var authGroup = app.MapGroup("/auth").WithTags("Auth");
        LogoutEndpoint.Map(authGroup);

        return app;
    }

    private static void ConfigureJwtBearer(JwtBearerOptions bearer, IOptions<JwtOptions> jwtOptions)
    {
        var jwt = jwtOptions.Value;
        if (string.IsNullOrEmpty(jwt.Key))
        {
            throw new InvalidOperationException("Jwt configuration is missing.");
        }

        bearer.MapInboundClaims = false;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            NameClaimType = JwtRegisteredClaimNames.Sub,
        };
        bearer.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var jti = ctx.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                if (jti is null)
                {
                    ctx.Fail("Missing jti.");
                    return;
                }
                var repo = ctx.HttpContext.RequestServices
                    .GetRequiredService<RevokedTokenRepository>();
                if (await repo.ExistsAsync(jti, ctx.HttpContext.RequestAborted))
                {
                    ctx.Fail("Token revoked.");
                }
            },
        };
    }
}
