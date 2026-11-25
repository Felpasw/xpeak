using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;

namespace Xpeak.Api.CheckIns.Repositories;

/// <summary>Author snapshot used by the detail read.</summary>
public sealed record CheckInAuthorSnapshot(
    Guid Id,
    string Username,
    string? AvatarUrl);

/// <summary>Detail read row — carries the check-in entity, its
/// author and the first media attachment (when any) so the endpoint
/// can project the response in one pass.</summary>
public sealed record CheckInDetailRow(
    CheckIn CheckIn,
    CheckInAuthorSnapshot Author,
    bool HasMedia,
    FirstMediaSource? FirstMedia);

/// <summary>
/// Read surface for <c>check_ins</c>. The list is scoped to a single
/// caller (there is no cross-user feed yet). The detail read is
/// gated by <c>ICheckInVisibility</c> at the endpoint layer, not here.
/// </summary>
public interface ICheckInRepository
{
    Task<CheckInPage> ListAsync(
        Guid userId,
        int limit,
        string? cursor,
        CancellationToken ct = default);

    Task<CheckInDetailRow?> GetDetailAsync(
        Guid checkInId,
        CancellationToken ct = default);
}
