using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xpeak.Api.CheckIns.Services;

namespace Xpeak.Api.Tests.CheckIns;

public sealed class StreakCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 9, 25);

    [Fact]
    public void Empty_list_returns_zero_zero_day()
    {
        var info = StreakCalculator.Compute([], Today);

        info.Should().Be(new StreakInfo(0, 0, StreakUnit.Day));
    }

    [Fact]
    public void Single_check_in_today_returns_one_one_day()
    {
        var info = StreakCalculator.Compute([Today], Today);

        info.Should().Be(new StreakInfo(1, 1, StreakUnit.Day));
    }

    [Fact]
    public void Three_consecutive_days_up_to_today_returns_three_three()
    {
        var dates = new[] { Today, Today.AddDays(-1), Today.AddDays(-2) };

        var info = StreakCalculator.Compute(dates, Today);

        info.Should().Be(new StreakInfo(3, 3, StreakUnit.Day));
    }

    [Fact]
    public void Gap_between_today_and_last_check_in_resets_current_to_zero()
    {
        var dates = new[] { Today.AddDays(-2), Today.AddDays(-3), Today.AddDays(-4) };

        var info = StreakCalculator.Compute(dates, Today);

        info.Current.Should().Be(0);
        info.Longest.Should().Be(3);
    }

    [Fact]
    public void Preserves_longest_after_a_break()
    {
        var dates = new[]
        {
            Today, Today.AddDays(-1),
            Today.AddDays(-10), Today.AddDays(-11),
            Today.AddDays(-12), Today.AddDays(-13),
            Today.AddDays(-14),
        };

        var info = StreakCalculator.Compute(dates, Today);

        info.Current.Should().Be(2);
        info.Longest.Should().Be(5);
    }

    [Fact]
    public void Yesterday_only_returns_zero_current_one_longest()
    {
        var info = StreakCalculator.Compute([Today.AddDays(-1)], Today);

        info.Should().Be(new StreakInfo(0, 1, StreakUnit.Day));
    }

    [Property]
    public bool Current_never_exceeds_longest(NonNegativeInt seed)
    {
        var n = seed.Get % 30;
        var dates = Enumerable.Range(0, n).Select(i => Today.AddDays(-i)).ToArray();

        var info = StreakCalculator.Compute(dates, Today);

        return info.Current <= info.Longest;
    }

    [Property]
    public bool N_consecutive_days_from_today_yields_current_n(NonNegativeInt seed)
    {
        var n = (seed.Get % 30) + 1;
        var dates = Enumerable.Range(0, n).Select(i => Today.AddDays(-i)).ToArray();

        var info = StreakCalculator.Compute(dates, Today);

        return info.Current == n && info.Longest == n;
    }
}
