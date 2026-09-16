namespace Xpeak.Api.Auth.Core;

/// <summary>Output of <see cref="JwtTokenIssuer.Issue"/>.</summary>
public readonly record struct IssuedToken(string Value, string Jti, DateTimeOffset ExpiresAt);
