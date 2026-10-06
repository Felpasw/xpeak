namespace Xpeak.Api.Media;

/// <summary>
/// Root media config. <see cref="Provider"/> selects which
/// <see cref="IMediaStorage"/> implementation gets wired at
/// startup. Add a new provider by extending <see cref="MediaModule"/>.
/// </summary>
public sealed class MediaOptions
{
    public const string SectionName = "Media";

    /// <summary>
    /// Provider identifier — case-insensitive. Supported values:
    /// <c>cloudinary</c> (default), <c>fake</c> (in-memory, for
    /// dev / tests without external credentials).
    /// </summary>
    public string Provider { get; set; } = "cloudinary";
}
