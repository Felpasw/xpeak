namespace Xpeak.Api.Media.Services;

/// <summary>
/// Tracks storage keys that we have handed out via presign but have
/// not yet been confirmed. Used by the confirm endpoint to accept
/// only keys we actually issued, and by the cleanup job to find
/// orphan uploads.
/// </summary>
public interface IPendingMediaCache
{
    void Register(string storageKey, Guid checkInId);

    /// <summary>Returns the associated <c>checkInId</c> if the key was
    /// registered, then removes it. Returns null if the key was never
    /// registered or has already been consumed / expired.</summary>
    Guid? Consume(string storageKey);
}
