using FsCheck;
using FsCheck.Xunit;
using Xpeak.Api.Progression.Services;

namespace Xpeak.Api.Tests.Progression;

public sealed class LevelUpServiceTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(99, 0)]
    [InlineData(100, 1)]
    [InlineData(282, 1)]
    [InlineData(283, 2)]
    [InlineData(300, 2)]
    [InlineData(519, 2)]
    [InlineData(520, 3)]
    [InlineData(3161, 9)]
    [InlineData(3162, 10)]
    [InlineData(100_000, 100)]
    public void LevelForXp_matches_the_reference_table(int xp, int expectedLevel)
    {
        LevelUpService.LevelForXp(xp).Should().Be(expectedLevel);
    }

    [Fact]
    public void LevelForXp_rejects_negative_xp()
    {
        var act = () => LevelUpService.LevelForXp(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Property]
    public bool LevelForXp_round_trips_with_XpForLevel(NonNegativeInt level)
    {
        var n = level.Get;
        if (n > 500)
        {
            return true;
        }

        var xp = LevelCurve.XpForLevel(n);
        return LevelUpService.LevelForXp(xp) == n;
    }

    [Fact]
    public void EvaluateLevelUp_flags_a_single_level_crossing()
    {
        var result = LevelUpService.EvaluateLevelUp(99, 100);

        result.Should().Be(new LevelUpResult(true, 1, 1));
    }

    [Fact]
    public void EvaluateLevelUp_reports_no_level_up_when_still_below_next_threshold()
    {
        var result = LevelUpService.EvaluateLevelUp(100, 199);

        result.Should().Be(new LevelUpResult(false, 1, 0));
    }

    [Fact]
    public void EvaluateLevelUp_reports_a_multi_level_jump()
    {
        // 0 → 3162 crosses levels 1, 2, ..., 10 in one shot.
        var result = LevelUpService.EvaluateLevelUp(0, 3_162);

        result.Should().Be(new LevelUpResult(true, 10, 10));
    }

    [Fact]
    public void EvaluateLevelUp_reports_no_change_when_xp_stays_the_same()
    {
        var result = LevelUpService.EvaluateLevelUp(500, 500);

        result.Should().Be(new LevelUpResult(false, 2, 0));
    }

    [Fact]
    public void EvaluateLevelUp_supports_a_zero_to_first_level_transition()
    {
        var result = LevelUpService.EvaluateLevelUp(0, 100);

        result.Should().Be(new LevelUpResult(true, 1, 1));
    }
}
