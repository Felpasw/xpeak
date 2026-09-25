using Xpeak.Api.CheckIns.Dto;

namespace Xpeak.Api.CheckIns.Repositories;

/// <summary>
/// Read surface for <c>check_ins</c>. All reads are scoped to a
/// single user; there is no cross-user endpoint. <c>groupId</c>
/// filter is optional — omit it to list across every group the
/// user has ever checked into.
/// </summary>
public interface ICheckInRepository
{
    Task<CheckInPage> ListAsync(
        Guid userId,
        int limit,
        string? cursor,
        Guid? groupId,
        CancellationToken ct = default);
}
