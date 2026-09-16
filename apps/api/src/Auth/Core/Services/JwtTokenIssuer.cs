using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xpeak.Api.Users;

namespace Xpeak.Api.Auth.Core;

/// <summary>
/// Issues signed JWTs for authenticated users. Each token gets a
/// random <c>jti</c> so it can be revoked individually via
/// <see cref="RevokedTokenRepository"/>.
/// </summary>
public sealed class JwtTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    private readonly JwtOptions _options = options.Value;
    private readonly TimeProvider _time = timeProvider;

    public IssuedToken Issue(AppUser user)
    {
        var jti = Guid.NewGuid().ToString("N");
        var now = _time.GetUtcNow();
        var expires = now + _options.Lifetime;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, jti),
            new("username", user.UserName ?? string.Empty),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        var encoded = new JwtSecurityTokenHandler().WriteToken(jwt);
        return new IssuedToken(encoded, jti, expires);
    }
}
