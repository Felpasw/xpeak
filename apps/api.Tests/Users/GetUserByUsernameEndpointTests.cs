using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Users;

public sealed class GetUserByUsernameEndpointTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public GetUserByUsernameEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Rejects_without_a_token_with_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/users/anyone");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Happy_path_returns_the_public_card()
    {
        var client = _factory.CreateClient();
        await Authenticate(client);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"pu_{suffix}";
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = new AppUser
            {
                UserName = username,
                Email = $"{username}@x.com",
                Background = Backgrounds.EtchedAccretion,
            };
            (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();
        }

        var response = await client.GetAsync($"/users/{username}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PublicUserDto>();
        body!.Username.Should().Be(username);
        body.Background.Should().Be("etched_accretion");
        body.Level.Should().Be(0);
    }

    [Fact]
    public async Task Lookup_is_case_insensitive()
    {
        var client = _factory.CreateClient();
        await Authenticate(client);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"ci_{suffix}";
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            (await users.CreateAsync(
                new AppUser { UserName = username, Email = $"{username}@x.com" },
                "hunter22!")).Succeeded.Should().BeTrue();
        }

        var response = await client.GetAsync($"/users/{username.ToUpperInvariant()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PublicUserDto>();
        body!.Username.Should().Be(username);
    }

    [Fact]
    public async Task Returns_404_when_user_does_not_exist()
    {
        var client = _factory.CreateClient();
        await Authenticate(client);

        var response = await client.GetAsync("/users/ghost_who_was_never_here");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task Authenticate(HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var register = await client.PostAsJsonAsync("/auth/register", new
        {
            username = $"caller_{suffix}",
            email = $"caller_{suffix}@x.com",
            password = "hunter22!",
        });
        register.EnsureSuccessStatusCode();
        var token = register.Headers.GetValues("Authorization").Single()["Bearer ".Length..];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}

internal sealed record PublicUserDto(
    Guid Id,
    string Username,
    string? AvatarUrl,
    int Level,
    int Xp,
    int CurrentStreakDays,
    int LongestStreakDays,
    string? Background,
    DateTimeOffset CreatedAt);
