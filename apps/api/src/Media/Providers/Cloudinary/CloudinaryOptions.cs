namespace Xpeak.Api.Media.Providers.Cloudinary;

/// <summary>
/// Cloudinary credentials + endpoints. Populated from the
/// <c>Cloudinary</c> section of the app configuration
/// (env-overridable via <c>Cloudinary__CloudName</c>, etc.).
/// </summary>
public sealed class CloudinaryOptions
{
    public string CloudName { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string ApiSecret { get; set; } = "";

    /// <summary>Base URL of the Cloudinary REST API. Overridable so tests
    /// can point it at a WireMock server.</summary>
    public string ApiBaseUrl { get; set; } = "https://api.cloudinary.com";
}
