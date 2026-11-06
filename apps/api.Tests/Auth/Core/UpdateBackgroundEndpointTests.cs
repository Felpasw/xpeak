using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Auth.Core;

public sealed class UpdateBackgroundEndpointTests : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public UpdateBackgroundEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Rejects_without_a_token_with_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsJsonAsync(
            "/me/background",
            new { background = "stars" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Happy_path_updates_the_field_and_echoes_the_new_me_payload()
    {
        var client = _factory.CreateClient();
        await Authenticate(client);

        var response = await client.PatchAsJsonAsync(
            "/me/background",
            new { background = "etched_accretion" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        body!.User.Background.Should().Be("etched_accretion");

        var me = await client.GetAsync("/me");
        var meBody = await me.Content.ReadFromJsonAsync<AuthResponseDto>();
        meBody!.User.Background.Should().Be("etched_accretion");
    }

    [Fact]
    public async Task Rejects_unknown_slug_with_422()
    {
        var client = _factory.CreateClient();
        await Authenticate(client);

        var response = await client.PatchAsJsonAsync(
            "/me/background",
            new { background = "cosmic_void" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Rejects_blank_slug_with_422()
    {
        var client = _factory.CreateClient();
        await Authenticate(client);

        var response = await client.PatchAsJsonAsync(
            "/me/background",
            new { background = "" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Rejects_missing_slug_with_422()
    {
        var client = _factory.CreateClient();
        await Authenticate(client);

        var response = await client.PatchAsJsonAsync("/me/background", new { });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Updates_background_when_current_username_is_reserved()
    {
        var client = _factory.CreateClient();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        Guid userId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                Email = $"reserved_{suffix}@x.com",
                NormalizedEmail = $"RESERVED_{suffix}@X.COM",
                SecurityStamp = Guid.NewGuid().ToString(),
                PasswordHash = "placeholder",
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = user.Id;
        }

        string token;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var issuer = scope.ServiceProvider.GetRequiredService<JwtTokenIssuer>();
            var user = new AppUser
            {
                Id = userId,
                UserName = "admin",
                Email = $"reserved_{suffix}@x.com",
            };
            token = issuer.Issue(user).Value;
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PatchAsJsonAsync(
            "/me/background",
            new { background = "stars" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        body!.User.Background.Should().Be("stars");
    }

    private static async Task Authenticate(HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var register = await client.PostAsJsonAsync("/auth/register", new
        {
            username = $"bg_{suffix}",
            email = $"bg_{suffix}@x.com",
            password = "hunter22!",
        });
        register.EnsureSuccessStatusCode();
        var token = register.Headers.GetValues("Authorization").Single()["Bearer ".Length..];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
