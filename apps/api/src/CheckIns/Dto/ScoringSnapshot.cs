namespace Xpeak.Api.CheckIns.Dto;

/// <summary>
/// Structured breakdown of how a single check-in's XP was computed.
/// Persisted as JSONB on <c>check_ins.scoring_snapshot</c> so the
/// audit trail is immutable even after admins edit categories or
/// rules later.
///
/// Phase 5 emits one multiplier entry (<c>category_weight</c>);
/// Phase 7 appends <c>streak</c>, <c>group_bonus</c> and
/// <c>challenge</c> without a schema change.
/// </summary>
public sealed record ScoringSnapshot(
    int BaseXp,
    IReadOnlyList<ScoringMultiplier> Multipliers,
    int Total);

public sealed record ScoringMultiplier(string Source, decimal Value);
