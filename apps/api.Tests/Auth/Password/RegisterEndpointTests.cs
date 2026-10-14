using System.Net;
using System.Net.Http.Json;
using Xpeak.Api.Tests.Support;

namespace Xpeak.Api.Tests.Auth.Password;

public sealed class RegisterEndpointTests : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public RegisterEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Happy_path_returns_201_with_authorization_header_and_body()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            username = $"newbie_{suffix}",
            email = $"newbie_{suffix}@x.com",
            password = "hunter22!",
        };

        var response = await client.PostAsJsonAsync("/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Contains("Authorization").Should().BeTrue();
        response.Headers.GetValues("Authorization").Single().Should().StartWith("Bearer ");

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        body.Should().NotBeNull();
        body!.User.Username.Should().Be(payload.username);
        body.User.Email.Should().Be(payload.email);
        body.User.Level.Should().Be(0);
        body.User.Xp.Should().Be(0);
    }

    [Fact]
    public async Task Rejects_invalid_username_format_with_422()
    {
        var client = _factory.CreateClient();
        var payload = new
        {
            username = "BadName!",
            email = $"bad_{Guid.NewGuid():N}@x.com",
            password = "hunter22!",
        };

        var response = await client.PostAsJsonAsync("/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Rejects_reserved_username_with_422()
    {
        var client = _factory.CreateClient();
        var payload = new
        {
            username = "admin",
            email = $"admin_{Guid.NewGuid():N}@x.com",
            password = "hunter22!",
        };

        var response = await client.PostAsJsonAsync("/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Rejects_weak_password_with_422()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            username = $"weak_{suffix}",
            email = $"weak_{suffix}@x.com",
            password = "short",
        };

        var response = await client.PostAsJsonAsync("/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Rejects_duplicate_username_with_422()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"twice_{suffix}";

        var first = await client.PostAsJsonAsync("/auth/register", new
        {
            username,
            email = $"first_{suffix}@x.com",
            password = "hunter22!",
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/auth/register", new
        {
            username,
            email = $"second_{suffix}@x.com",
            password = "hunter22!",
        });

        second.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Rejects_duplicate_email_with_422()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"shared_{suffix}@x.com";

        var first = await client.PostAsJsonAsync("/auth/register", new
        {
            username = $"one_{suffix}",
            email,
            password = "hunter22!",
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/auth/register", new
        {
            username = $"two_{suffix}",
            email,
            password = "hunter22!",
        });

        second.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
