using Xpeak.Api.Challenges.Entities;
using Xpeak.Api.Challenges.Services;

namespace Xpeak.Api.Tests.Challenges;

public sealed class ChallengeTierResolverTests
{
    [Fact]
    public void Returns_null_for_an_empty_list()
    {
        var result = ChallengeTierResolver.ResolveCurrent([], 42);

        result.Should().BeNull();
    }

    [Fact]
    public void Returns_null_when_progress_is_below_the_first_tier()
    {
        var tiers = Tiers(10, 30, 60);

        var result = ChallengeTierResolver.ResolveCurrent(tiers, 5);

        result.Should().BeNull();
    }

    [Fact]
    public void Returns_the_first_tier_on_an_exact_match()
    {
        var tiers = Tiers(10, 30, 60);

        var result = ChallengeTierResolver.ResolveCurrent(tiers, 10);

        result!.MinLevel.Should().Be(10);
    }

    [Fact]
    public void Returns_the_lower_tier_when_progress_is_between_two()
    {
        var tiers = Tiers(10, 30, 60);

        var result = ChallengeTierResolver.ResolveCurrent(tiers, 29);

        result!.MinLevel.Should().Be(10);
    }

    [Fact]
    public void Returns_the_second_tier_on_an_exact_match()
    {
        var tiers = Tiers(10, 30, 60);

        var result = ChallengeTierResolver.ResolveCurrent(tiers, 30);

        result!.MinLevel.Should().Be(30);
    }

    [Fact]
    public void Returns_the_last_tier_when_progress_passes_every_floor()
    {
        var tiers = Tiers(10, 30, 60);

        var result = ChallengeTierResolver.ResolveCurrent(tiers, 999);

        result!.MinLevel.Should().Be(60);
    }

    [Fact]
    public void Returns_the_only_tier_when_the_list_has_a_single_entry()
    {
        var tiers = Tiers(1);

        var result = ChallengeTierResolver.ResolveCurrent(tiers, 1);

        result!.MinLevel.Should().Be(1);
    }

    private static IReadOnlyList<ChallengeTier> Tiers(params int[] minLevels)
    {
        var list = new List<ChallengeTier>(minLevels.Length);
        foreach (var minLevel in minLevels)
        {
            list.Add(new ChallengeTier
            {
                Id = Guid.NewGuid(),
                ChallengeId = Guid.NewGuid(),
                MinLevel = minLevel,
                Title = $"Tier {minLevel}",
            });
        }
        return list;
    }
}
