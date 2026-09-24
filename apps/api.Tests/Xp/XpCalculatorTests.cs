using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Xp;

namespace Xpeak.Api.Tests.Xp;

public sealed class XpCalculatorTests
{
    [Theory]
    [InlineData(15, 1.40, 21)]
    [InlineData(12, 1.20, 14)]
    [InlineData(8, 1.00, 8)]
    [InlineData(6, 0.80, 5)]
    [InlineData(5, 1.00, 5)]
    [InlineData(10, 0.50, 5)]
    [InlineData(10, 2.50, 25)]
    public void Compute_matches_expected_rounding(int baseXp, double weight, int expected)
    {
        var rule = new XpRule { BaseXp = baseXp, WeightMultiplier = (decimal)weight };

        XpCalculator.Compute(rule).Should().Be(expected);
    }

    [Fact]
    public void Compute_rounds_half_away_from_zero_not_banker()
    {
        // 5 * 0.9 = 4.5 → banker's rounding gives 4, AwayFromZero gives 5.
        // The whole point of the rounding decision in ADR-0004.
        var rule = new XpRule { BaseXp = 5, WeightMultiplier = 0.90m };

        XpCalculator.Compute(rule).Should().Be(5);
    }

    [Property]
    public bool Compute_is_strictly_positive_for_any_valid_rule(
        PositiveInt baseXp,
        int weightHundredths)
    {
        if (baseXp.Get > 100_000)
        {
            return true;
        }

        if (weightHundredths < 50 || weightHundredths > 250)
        {
            return true;
        }

        var rule = new XpRule
        {
            BaseXp = baseXp.Get,
            WeightMultiplier = weightHundredths / 100m,
        };
        return XpCalculator.Compute(rule) > 0;
    }

    [Property]
    public bool Compute_is_monotonic_on_base_xp_when_weight_is_fixed(
        PositiveInt baseXp,
        int weightHundredths)
    {
        if (baseXp.Get > 10_000)
        {
            return true;
        }

        if (weightHundredths < 50 || weightHundredths > 250)
        {
            return true;
        }

        var weight = weightHundredths / 100m;
        var smaller = new XpRule { BaseXp = baseXp.Get, WeightMultiplier = weight };
        var bigger = new XpRule { BaseXp = baseXp.Get + 1, WeightMultiplier = weight };
        return XpCalculator.Compute(bigger) >= XpCalculator.Compute(smaller);
    }
}
