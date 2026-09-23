namespace Xpeak.Api.Auth.Google;

/// <summary>Configuration for the Google OAuth flow.</summary>
public sealed class GoogleOptions
{
    public const string SectionName = "Google";

    public string ClientId { get; init; } = "unset";

    public string ClientSecret { get; init; } = "unset";
}
