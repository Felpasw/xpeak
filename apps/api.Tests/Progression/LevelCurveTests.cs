using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xpeak.Api.Progression.Services;

namespace Xpeak.Api.Tests.Progression;

public sealed class LevelCurveTests
{
    [Fact]
    public void XpForLevel_zero_is_zero()
    {
        LevelCurve.XpForLevel(0).Should().Be(0);
    }

    [Theory]
    [InlineData(1, 100)]
    [InlineData(5, 1118)]
    [InlineData(10, 3162)]
    [InlineData(50, 35355)]
    [InlineData(100, 100000)]
    public void XpForLevel_matches_the_reference_table(int level, int expected)
    {
        LevelCurve.XpForLevel(level).Should().Be(expected);
    }

    [Fact]
    public void XpForLevel_rejects_negative_input()
    {
        var act = () => LevelCurve.XpForLevel(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Property]
    public bool XpForLevel_is_strictly_increasing_for_positive_levels(PositiveInt level)
    {
        var n = level.Get;
        if (n > 1_000_000)
        {
            return true;
        }

        return LevelCurve.XpForLevel(n + 1) > LevelCurve.XpForLevel(n);
    }

    [Property]
    public bool XpForLevel_grows_monotonically_from_zero(NonNegativeInt level)
    {
        var n = level.Get;
        if (n > 1_000_000)
        {
            return true;
        }

        return LevelCurve.XpForLevel(n + 1) >= LevelCurve.XpForLevel(n);
    }
}
