using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;
using Xpeak.Api.Xp;

namespace Xpeak.Api.Tests.CheckIns;

public sealed class CreateCheckInEndpointTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public CreateCheckInEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Happy_path_returns_201_and_awards_xp()
    {
        var user = await SeedUserAsync();
        var client = AuthenticatedClientFor(user);

        var response = await client.PostAsJsonAsync("/check_ins", new
        {
            title = "leg day",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<CreateCheckInResponseDto>();
        body.Should().NotBeNull();
        body!.CheckIn.XpEarned.Should().Be(XpCalculator.CheckInXp);
        body.User.Xp.Should().Be(XpCalculator.CheckInXp);
        body.Streak.Current.Should().Be(1);
    }

    [Fact]
    public async Task Pending_path_returns_201_with_zero_xp_when_with_media_is_true()
    {
        var user = await SeedUserAsync();
        var client = AuthenticatedClientFor(user);

        var response = await client.PostAsJsonAsync("/check_ins", new
        {
            title = "with media",
            withMedia = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<CreateCheckInResponseDto>();
        body.Should().NotBeNull();
        body!.CheckIn.XpEarned.Should().Be(0);
        body.CheckIn.HasMedia.Should().BeFalse();

        body.User.Xp.Should().Be(0);
        body.User.LeveledUp.Should().BeFalse();
        body.Streak.Current.Should().Be(0);
    }

    [Fact]
    public async Task Rejects_without_a_token_with_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/check_ins", new
        {
            title = "test",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Rejects_blank_title_with_422()
    {
        var user = await SeedUserAsync();
        var client = AuthenticatedClientFor(user);

        var response = await client.PostAsJsonAsync("/check_ins", new
        {
            title = "   ",
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Rejects_with_422_when_duration_minutes_is_not_positive()
    {
        var user = await SeedUserAsync();
        var client = AuthenticatedClientFor(user);

        var response = await client.PostAsJsonAsync("/check_ins", new
        {
            title = "test",
            durationMinutes = 0,
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private sealed record CreateCheckInResponseDto(
        CheckInBody CheckIn,
        UserBody User,
        StreakBody Streak);

    private sealed record CheckInBody(
        Guid Id,
        int XpEarned,
        ScoringSnapshotBody ScoringSnapshot,
        DateTimeOffset PerformedAt,
        int? DurationMinutes,
        string? Notes,
        bool HasMedia);

    private sealed record ScoringSnapshotBody(
        int BaseXp,
        IReadOnlyList<MultiplierBody> Multipliers,
        int Total);

    private sealed record MultiplierBody(string Source, decimal Value);

    private sealed record UserBody(
        Guid Id,
        int Xp,
        int Level,
        bool LeveledUp,
        int LevelsGained);

    private sealed record StreakBody(int Current, int Longest, string Unit);

    private HttpClient AuthenticatedClientFor(AppUser user)
    {
        using var scope = _factory.Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<JwtTokenIssuer>();
        var token = issuer.Issue(user);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        return client;
    }

    private async Task<AppUser> SeedUserAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"ep_{suffix}", Email = $"ep_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();
        return user;
    }
}
