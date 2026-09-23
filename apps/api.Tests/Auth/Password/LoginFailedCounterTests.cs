using System.Net;
using System.Net.Http.Json;
using Xpeak.Api.Tests.Support;

namespace Xpeak.Api.Tests.Auth.Password;

public sealed class LoginFailedCounterLocksTests : IClassFixture<RateLimitedXpeakWebApplicationFactory>
{
    private readonly RateLimitedXpeakWebApplicationFactory _factory;

    public LoginFailedCounterLocksTests(RateLimitedXpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Sixth_failed_login_for_same_identifier_returns_429()
    {
        var client = _factory.CreateClient();
        var email = await Register(client);

        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/auth/login", new
            {
                identifier = email,
                password = "wrong!",
            });
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var sixth = await client.PostAsJsonAsync("/auth/login", new
        {
            identifier = email,
            password = "wrong!",
        });
        sixth.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    private static async Task<string> Register(HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"rl_login_{suffix}@x.com";
        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            username = $"rl_login_{suffix}",
            email,
            password = "hunter22!",
        });
        response.EnsureSuccessStatusCode();
        return email;
    }
}

public sealed class LoginSuccessClearsFailedCounterTests : IClassFixture<RateLimitedXpeakWebApplicationFactory>
{
    private readonly RateLimitedXpeakWebApplicationFactory _factory;

    public LoginSuccessClearsFailedCounterTests(RateLimitedXpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Successful_login_resets_failed_counter_for_that_identifier()
    {
        var client = _factory.CreateClient();
        var (email, password) = await Register(client);

        for (var i = 0; i < 4; i++)
        {
            var fail = await client.PostAsJsonAsync("/auth/login", new
            {
                identifier = email,
                password = "wrong!",
            });
            fail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var success = await client.PostAsJsonAsync("/auth/login", new
        {
            identifier = email,
            password,
        });
        success.StatusCode.Should().Be(HttpStatusCode.OK);

        var followUpFail = await client.PostAsJsonAsync("/auth/login", new
        {
            identifier = email,
            password = "wrong!",
        });
        followUpFail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<(string Email, string Password)> Register(HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"rl_ok_{suffix}@x.com";
        const string password = "hunter22!";
        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            username = $"rl_ok_{suffix}",
            email,
            password,
        });
        response.EnsureSuccessStatusCode();
        return (email, password);
    }
}
