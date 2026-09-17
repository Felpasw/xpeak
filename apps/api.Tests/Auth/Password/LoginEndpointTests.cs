using System.Net;
using System.Net.Http.Json;
using Xpeak.Api.Tests.Support;

namespace Xpeak.Api.Tests.Auth.Password;

public sealed class LoginEndpointTests : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public LoginEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_via_email_returns_200_with_bearer_token()
    {
        var client = _factory.CreateClient();
        var (username, email, password) = await Register(client);

        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            identifier = email,
            password,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("Authorization").Single().Should().StartWith("Bearer ");
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        body!.User.Username.Should().Be(username);
    }

    [Fact]
    public async Task Login_via_username_returns_200_with_bearer_token()
    {
        var client = _factory.CreateClient();
        var (username, _, password) = await Register(client);

        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            identifier = username,
            password,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("Authorization").Single().Should().StartWith("Bearer ");
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var client = _factory.CreateClient();
        var (_, email, _) = await Register(client);

        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            identifier = email,
            password = "wrong-password!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_with_unknown_identifier_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            identifier = $"nobody_{Guid.NewGuid():N}@x.com",
            password = "hunter22!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_with_empty_identifier_returns_422()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            identifier = string.Empty,
            password = "hunter22!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private static async Task<(string Username, string Email, string Password)> Register(HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"login_{suffix}";
        var email = $"login_{suffix}@x.com";
        const string password = "hunter22!";

        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            username,
            email,
            password,
        });
        response.EnsureSuccessStatusCode();
        return (username, email, password);
    }
}
