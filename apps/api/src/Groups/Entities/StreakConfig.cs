namespace Xpeak.Api.Groups.Entities;

/// <summary>
/// How a group measures streaks. Phase 5 only implements
/// <see cref="Daily"/>; <see cref="Weekly"/> is schema-ready but
/// the calculator throws until Phase 9 / Phase 11 wires it up.
/// </summary>
public enum StreakMode
{
    Daily,
    Weekly,
}

/// <summary>
/// Per-group streak configuration. Serialized as JSONB inside
/// <see cref="GroupConfig.StreakConfig"/>.
///
/// Weekly-mode fields (<see cref="RequiredDaysPerWeek"/>,
/// <see cref="WeekStart"/>) are only meaningful when
/// <see cref="Mode"/> is <see cref="StreakMode.Weekly"/> — for
/// daily mode they stay null.
/// </summary>
public sealed record StreakConfig(
    StreakMode Mode,
    int? RequiredDaysPerWeek = null,
    DayOfWeek? WeekStart = null);
