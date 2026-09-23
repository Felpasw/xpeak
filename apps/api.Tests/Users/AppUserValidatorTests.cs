using Microsoft.AspNetCore.Identity;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Users;

public sealed class AppUserValidatorTests
{
    private readonly AppUserValidator _validator = new();

    [Fact]
    public async Task Rejects_missing_username()
    {
        var result = await _validator.ValidateAsync(null!, new AppUser { UserName = null });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "UsernameRequired");
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("thisusernameiswaytoolongforus")]
    [InlineData("Felipe")]
    [InlineData("felipe.souza")]
    [InlineData("felipe souza")]
    [InlineData("felipe@souza")]
    public async Task Rejects_invalid_format(string username)
    {
        var result = await _validator.ValidateAsync(null!, new AppUser { UserName = username });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "UsernameFormat");
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("root")]
    [InlineData("me")]
    [InlineData("xpeak")]
    public async Task Rejects_reserved_usernames(string username)
    {
        var result = await _validator.ValidateAsync(null!, new AppUser { UserName = username });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "UsernameReserved");
    }

    [Theory]
    [InlineData("felipe")]
    [InlineData("felipe_99")]
    [InlineData("abc")]
    [InlineData("thisusernameiswaytoo")]
    public async Task Accepts_valid_usernames(string username)
    {
        var result = await _validator.ValidateAsync(null!, new AppUser { UserName = username });

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void AppUser_defaults_progression_fields_to_zero()
    {
        var user = new AppUser();

        user.Level.Should().Be(0);
        user.Xp.Should().Be(0);
    }
}
