using Xpeak.Api.Progression.Entities;

namespace Xpeak.Api.Progression.Repositories;

/// <summary>
/// Read surface for <see cref="XpRule"/>. Phase 5 only needs a
/// by-id lookup so the check-in flow can hand a rule to
/// <c>XpCalculator.Compute</c>. Create / update / list arrive with
/// the Phase 9 admin surface.
/// </summary>
public interface IXpRuleRepository
{
    Task<XpRule?> GetByIdAsync(Guid ruleId, CancellationToken ct = default);
}
