using Xpeak.Api.Challenges.Dto;
using Xpeak.Api.Challenges.Entities;

namespace Xpeak.Api.Tests.Challenges;

public sealed class CreateChallengeRequestValidatorTests
{
    private static readonly CreateChallengeRequestValidator Validator = new();

    [Fact]
    public void Null_tiers_is_valid()
    {
        var request = NewRequest(tiers: null);

        var result = Validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_tiers_is_valid()
    {
        var request = NewRequest(tiers: []);

        var result = Validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_eleven_tiers()
    {
        var tiers = Enumerable.Range(1, 11)
            .Select(i => new CreateChallengeTierRequest(MinLevel: i, Title: $"Tier {i}"))
            .ToList();
        var request = NewRequest(tiers: tiers);

        var result = Validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreateChallengeRequest.Tiers)
            && e.ErrorMessage.Contains("10 entries or fewer"));
    }

    [Fact]
    public void Rejects_duplicate_min_level()
    {
        var request = NewRequest(tiers:
        [
            new(MinLevel: 10, Title: "Aprendiz"),
            new(MinLevel: 10, Title: "Outro"),
        ]);

        var result = Validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreateChallengeRequest.Tiers)
            && e.ErrorMessage.Contains("minLevel"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000)]
    public void Rejects_min_level_outside_the_valid_range(int minLevel)
    {
        var request = NewRequest(tiers:
        [
            new(MinLevel: minLevel, Title: "Mago"),
        ]);

        var result = Validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("minLevel must be between"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_blank_tier_title(string blank)
    {
        var request = NewRequest(tiers:
        [
            new(MinLevel: 10, Title: blank),
        ]);

        var result = Validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_tier_title_longer_than_sixty_chars()
    {
        var request = NewRequest(tiers:
        [
            new(MinLevel: 10, Title: new string('x', 61)),
        ]);

        var result = Validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage.Contains("60 characters or fewer"));
    }

    [Fact]
    public void Accepts_three_tiers_with_valid_values()
    {
        var request = NewRequest(tiers:
        [
            new(MinLevel: 10, Title: "Aprendiz"),
            new(MinLevel: 30, Title: "Mago"),
            new(MinLevel: 60, Title: "ArchMago"),
        ]);

        var result = Validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    private static CreateChallengeRequest NewRequest(IReadOnlyList<CreateChallengeTierRequest>? tiers)
    {
        var now = DateTimeOffset.UtcNow;
        return new CreateChallengeRequest(
            Title: "100 dias",
            Description: null,
            IconPublicId: null,
            Visibility: ChallengeVisibility.Private,
            TargetLevel: 60,
            XpMultiplier: 1.0m,
            StartsAt: now,
            EndsAt: now.AddDays(100),
            MemberCap: null,
            Tiers: tiers);
    }
}
