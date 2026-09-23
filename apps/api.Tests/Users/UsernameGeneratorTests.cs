using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Users;

public sealed class UsernameGeneratorTests
{
    [Theory]
    [InlineData("Felipe", "felipe")]
    [InlineData("felipe.souza", "felipesouza")]
    [InlineData("FELIPE_99", "felipe_99")]
    [InlineData("  spaced  ", "spaced")]
    [InlineData("mário", "mrio")]
    [InlineData("!@#", "_user")]
    [InlineData("ab", "ab_user")]
    [InlineData("thisusernameiswaytoolongforus", "thisusernameiswaytoo")]
    public void Sanitize_normalises_input(string input, string expected)
    {
        UsernameGenerator.Sanitize(input).Should().Be(expected);
    }

    [Fact]
    public void Sanitize_returns_default_for_null_or_whitespace()
    {
        UsernameGenerator.Sanitize(string.Empty).Should().Be("user");
        UsernameGenerator.Sanitize("   ").Should().Be("user");
    }

    [Fact]
    public async Task GenerateAsync_returns_base_when_free()
    {
        var result = await UsernameGenerator.GenerateAsync(
            seed: "felipe",
            usernameExistsAsync: (_, _) => Task.FromResult(false));

        result.Should().Be("felipe");
    }

    [Fact]
    public async Task GenerateAsync_appends_numeric_suffix_on_collision()
    {
        var taken = new HashSet<string>(StringComparer.Ordinal) { "felipe", "felipe_2" };

        var result = await UsernameGenerator.GenerateAsync(
            seed: "felipe",
            usernameExistsAsync: (candidate, _) => Task.FromResult(taken.Contains(candidate)));

        result.Should().Be("felipe_3");
    }

    [Fact]
    public async Task GenerateAsync_skips_reserved_names()
    {
        var result = await UsernameGenerator.GenerateAsync(
            seed: "admin",
            usernameExistsAsync: (_, _) => Task.FromResult(false));

        result.Should().Be("admin_2");
    }

    [Fact]
    public async Task GenerateAsync_truncates_base_to_fit_suffix()
    {
        var taken = new HashSet<string>(StringComparer.Ordinal)
        {
            "thisusernameiswaytoo",
        };

        var result = await UsernameGenerator.GenerateAsync(
            seed: "thisusernameiswaytoolongforus",
            usernameExistsAsync: (candidate, _) => Task.FromResult(taken.Contains(candidate)));

        result.Length.Should().BeLessThanOrEqualTo(20);
        result.Should().EndWith("_2");
    }

    [Fact]
    public async Task GenerateAsync_pads_short_seed_before_suffixing()
    {
        var result = await UsernameGenerator.GenerateAsync(
            seed: "ab",
            usernameExistsAsync: (_, _) => Task.FromResult(false));

        result.Should().Be("ab_user");
    }
}
