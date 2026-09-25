using Xpeak.Api.Groups.Entities;

namespace Xpeak.Api.Groups.Services;

/// <summary>
/// Read surface for <see cref="GroupConfig"/>. Every group is
/// expected to have exactly one config row (seeded for Global,
/// created on group creation from Phase 12+ onward); the service
/// throws if that invariant is ever violated so we fail loud
/// instead of silently defaulting.
/// </summary>
public interface IGroupConfigService
{
    Task<StreakConfig> GetStreakConfigAsync(Guid groupId, CancellationToken ct = default);
}
