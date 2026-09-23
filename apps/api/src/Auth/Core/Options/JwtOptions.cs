namespace Xpeak.Api.Auth.Core;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Key { get; init; }

    public string Issuer { get; init; } = "xpeak";

    public string Audience { get; init; } = "xpeak";

    public TimeSpan Lifetime { get; init; } = TimeSpan.FromDays(30);
}
