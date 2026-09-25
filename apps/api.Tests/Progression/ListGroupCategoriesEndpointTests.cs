using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Progression;

public sealed class ListGroupCategoriesEndpointTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public ListGroupCategoriesEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Rejects_without_a_token_with_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/groups/{Guid.NewGuid()}/categories");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_active_categories_of_the_group_ordered_by_slug()
    {
        var user = await CreateUserAsync();
        var group = await CreateGroupAsync();
        var rule = await CreateRuleAsync();
        await CreateCategoryAsync(group.Id, rule.Id, "chest", "Chest", active: true);
        await CreateCategoryAsync(group.Id, rule.Id, "back", "Back", active: true);
        await CreateCategoryAsync(group.Id, rule.Id, "legs", "Legs", active: false);
        var client = AuthenticatedClientFor(user);

        var response = await client.GetAsync($"/groups/{group.Id}/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListBody>();
        body.Should().NotBeNull();
        body!.Categories.Should().HaveCount(2);
        body.Categories.Select(c => c.Slug).Should().BeEquivalentTo(new[] { "back", "chest" });
    }

    [Fact]
    public async Task Returns_empty_list_when_the_group_has_no_categories()
    {
        var user = await CreateUserAsync();
        var group = await CreateGroupAsync();
        var client = AuthenticatedClientFor(user);

        var body = await client.GetFromJsonAsync<ListBody>($"/groups/{group.Id}/categories");

        body.Should().NotBeNull();
        body!.Categories.Should().BeEmpty();
    }

    private sealed record ListBody(IReadOnlyList<CategoryBody> Categories);

    private sealed record CategoryBody(
        Guid Id,
        Guid GroupId,
        string Slug,
        string Name,
        string? IconPublicId,
        bool Active);

    private HttpClient AuthenticatedClientFor(AppUser user)
    {
        using var scope = _factory.Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<JwtTokenIssuer>();
        var token = issuer.Issue(user);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        return client;
    }

    private async Task<AppUser> CreateUserAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var user = new AppUser { UserName = $"lgc_{suffix}", Email = $"lgc_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();
        return user;
    }

    private async Task<Group> CreateGroupAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var group = new Group { Id = Guid.NewGuid(), Name = $"g_{suffix}", IsRoot = false };
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        return group;
    }

    private async Task<XpRule> CreateRuleAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rule = new XpRule { Id = Guid.NewGuid(), BaseXp = 10, WeightMultiplier = 1.0m };
        db.XpRules.Add(rule);
        await db.SaveChangesAsync();
        return rule;
    }

    private async Task CreateCategoryAsync(Guid groupId, Guid ruleId, string slug, string name, bool active)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Slug uniqueness is `(group_id, slug)` composite — each test
        // creates fresh groups so bare slugs never collide.
        db.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            XpRuleId = ruleId,
            Slug = slug,
            Name = name,
            Active = active,
        });
        await db.SaveChangesAsync();
    }
}
