using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;

namespace Xpeak.Api.Tests.Auth.Core;

public sealed class LogoutEndpointTests : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public LogoutEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Returns_401_without_token()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/auth/logout", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revokes_jti_and_returns_204_when_authenticated()
    {
        var client = _factory.CreateClient();
        var token = await RegisterAndGetToken(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.PostAsync("/auth/logout", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jti = ReadJtiFromToken(token);
        var stored = await db.RevokedTokens.AsNoTracking().SingleOrDefaultAsync(t => t.Jti == jti);
        stored.Should().NotBeNull();
    }

    [Fact]
    public async Task Second_logout_with_same_token_is_rejected()
    {
        var client = _factory.CreateClient();
        var token = await RegisterAndGetToken(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        (await client.PostAsync("/auth/logout", content: null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var second = await client.PostAsync("/auth/logout", content: null);
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<string> RegisterAndGetToken(HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            username = $"lo_{suffix}",
            email = $"lo_{suffix}@x.com",
            password = "hunter22!",
        });
        response.EnsureSuccessStatusCode();
        var header = response.Headers.GetValues("Authorization").Single();
        return header["Bearer ".Length..];
    }

    private static string ReadJtiFromToken(string jwt)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(jwt);
        return parsed.Payload["jti"].ToString()!;
    }
}
