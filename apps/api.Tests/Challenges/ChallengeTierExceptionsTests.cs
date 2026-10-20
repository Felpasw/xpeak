using Xpeak.Api.Challenges.Services;

namespace Xpeak.Api.Tests.Challenges;

public sealed class ChallengeTierExceptionsTests
{
    [Fact]
    public void Too_many_tiers_exception_carries_the_counts_and_message()
    {
        var challengeId = Guid.NewGuid();

        var ex = new TooManyTiersException(challengeId, count: 11, max: 10);

        ex.ChallengeId.Should().Be(challengeId);
        ex.Count.Should().Be(11);
        ex.Max.Should().Be(10);
        ex.Message.Should().Contain(challengeId.ToString());
        ex.Message.Should().Contain("11");
        ex.Message.Should().Contain("10");
    }

    [Fact]
    public void Duplicate_tier_exception_carries_the_min_level_and_message()
    {
        var challengeId = Guid.NewGuid();

        var ex = new DuplicateTierException(challengeId, minLevel: 30);

        ex.ChallengeId.Should().Be(challengeId);
        ex.MinLevel.Should().Be(30);
        ex.Message.Should().Contain(challengeId.ToString());
        ex.Message.Should().Contain("30");
    }
}
