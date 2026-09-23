using System.Net;
using System.Net.Http.Json;
using Xpeak.Api.Tests.Support;

namespace Xpeak.Api.Tests.Auth.Password;

public sealed class RegisterRateLimitTests : IClassFixture<RateLimitedXpeakWebApplicationFactory>
{
    private readonly RateLimitedXpeakWebApplicationFactory _factory;

    public RegisterRateLimitTests(RateLimitedXpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Fourth_register_within_the_window_returns_429()
    {
        var client = _factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var response = await client.PostAsJsonAsync("/auth/register", new
            {
                username = $"rl_{suffix}",
                email = $"rl_{suffix}@x.com",
                password = "hunter22!",
            });
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var fourthSuffix = Guid.NewGuid().ToString("N")[..8];
        var fourth = await client.PostAsJsonAsync("/auth/register", new
        {
            username = $"rl_{fourthSuffix}",
            email = $"rl_{fourthSuffix}@x.com",
            password = "hunter22!",
        });

        fourth.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
