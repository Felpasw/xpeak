namespace Xpeak.Api.Media.Services;

/// <summary>Thrown when a media operation targets a check-in that does not exist.</summary>
public sealed class CheckInNotFoundException(Guid checkInId)
    : Exception($"Check-in '{checkInId}' does not exist.")
{
    public Guid CheckInId { get; } = checkInId;
}

/// <summary>Thrown when the caller is not the owner of the target check-in.</summary>
public sealed class CheckInNotOwnedException(Guid checkInId, Guid userId)
    : Exception($"User '{userId}' does not own check-in '{checkInId}'.")
{
    public Guid CheckInId { get; } = checkInId;
    public Guid UserId { get; } = userId;
}

/// <summary>Thrown when a confirm submits a storage_key we never issued
/// (or that expired / was already consumed), or when the storage backend
/// has no asset under that key. Endpoint layer maps to HTTP 422.</summary>
public sealed class UnknownStorageKeyException(string storageKey)
    : Exception($"Storage key '{storageKey}' was not issued by presign or has no asset.")
{
    public string StorageKey { get; } = storageKey;
}

/// <summary>Thrown when a confirm submits a storage_key issued for a
/// different check-in than the one in the URL. Endpoint layer maps to
/// HTTP 422.</summary>
public sealed class StorageKeyCheckInMismatchException(string storageKey, Guid expected, Guid actual)
    : Exception($"Storage key '{storageKey}' was issued for check-in '{expected}', not '{actual}'.")
{
    public string StorageKey { get; } = storageKey;
    public Guid Expected { get; } = expected;
    public Guid Actual { get; } = actual;
}

/// <summary>Thrown when the referenced media row does not exist under
/// the given check-in. Endpoint layer maps to HTTP 404.</summary>
public sealed class MediaNotFoundException(Guid mediaId, Guid checkInId)
    : Exception($"Media '{mediaId}' not found under check-in '{checkInId}'.")
{
    public Guid MediaId { get; } = mediaId;
    public Guid CheckInId { get; } = checkInId;
}
