using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xpeak.Api.Tests.Support;

namespace Xpeak.Api.Tests.Auth.Core;

public sealed class MeEndpointTests : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public MeEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Returns_401_without_token()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_200_with_user_body_for_valid_token()
    {
        var client = _factory.CreateClient();
        var (username, token) = await RegisterAndGetToken(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        body!.User.Username.Should().Be(username);
    }

    [Fact]
    public async Task Returns_401_after_token_is_revoked()
    {
        var client = _factory.CreateClient();
        var (_, token) = await RegisterAndGetToken(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var logout = await client.PostAsync("/auth/logout", content: null);
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var me = await client.GetAsync("/me");
        me.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<(string Username, string Token)> RegisterAndGetToken(HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"me_{suffix}";
        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            username,
            email = $"me_{suffix}@x.com",
            password = "hunter22!",
        });
        response.EnsureSuccessStatusCode();
        var header = response.Headers.GetValues("Authorization").Single();
        var token = header["Bearer ".Length..];
        return (username, token);
    }
}
