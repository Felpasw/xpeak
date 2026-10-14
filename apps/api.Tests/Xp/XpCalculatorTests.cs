using Xpeak.Api.Xp;

namespace Xpeak.Api.Tests.Xp;

public sealed class XpCalculatorTests
{
    [Fact]
    public void CheckInXp_is_the_hardcoded_payout_per_check_in()
    {
        XpCalculator.CheckInXp.Should().Be(10);
    }

    [Fact]
    public void Compute_returns_the_check_in_xp_constant()
    {
        XpCalculator.Compute().Should().Be(XpCalculator.CheckInXp);
    }
}
