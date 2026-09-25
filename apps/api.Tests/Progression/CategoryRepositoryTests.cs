using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Groups;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Progression.Repositories;
using Xpeak.Api.Tests.Support;

namespace Xpeak.Api.Tests.Progression;

public sealed class CategoryRepositoryTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public CategoryRepositoryTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task ListActiveAsync_excludes_inactive_rows()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var group = await CreateGroupAsync(db, $"list_{suffix}");
        var rule = await CreateRuleAsync(db);

        var active = new Category
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            XpRuleId = rule.Id,
            Slug = $"active_{suffix}",
            Name = "Active",
            Active = true,
        };
        var inactive = new Category
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            XpRuleId = rule.Id,
            Slug = $"inactive_{suffix}",
            Name = "Inactive",
            Active = false,
        };
        db.Categories.AddRange(active, inactive);
        await db.SaveChangesAsync();

        var result = await repo.ListActiveAsync(group.Id);

        result.Should().ContainSingle().Which.Id.Should().Be(active.Id);
    }

    [Fact]
    public async Task ListActiveAsync_scopes_reads_to_the_requested_group()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var mine = await CreateGroupAsync(db, $"mine_{suffix}");
        var other = await CreateGroupAsync(db, $"other_{suffix}");
        var rule = await CreateRuleAsync(db);

        db.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            GroupId = mine.Id,
            XpRuleId = rule.Id,
            Slug = $"mycat_{suffix}",
            Name = "Mine",
        });
        db.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            GroupId = other.Id,
            XpRuleId = rule.Id,
            Slug = $"othercat_{suffix}",
            Name = "Other",
        });
        await db.SaveChangesAsync();

        var mineResult = await repo.ListActiveAsync(mine.Id);
        var otherResult = await repo.ListActiveAsync(other.Id);

        mineResult.Should().ContainSingle().Which.Name.Should().Be("Mine");
        otherResult.Should().ContainSingle().Which.Name.Should().Be("Other");
    }

    [Fact]
    public async Task GetBySlugAsync_matches_case_insensitively_via_citext()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var group = await CreateGroupAsync(db, $"cs_{suffix}");
        var rule = await CreateRuleAsync(db);

        var slug = $"legs_{suffix}";
        db.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            XpRuleId = rule.Id,
            Slug = slug,
            Name = "Legs",
        });
        await db.SaveChangesAsync();

        var byUpper = await repo.GetBySlugAsync(group.Id, slug.ToUpperInvariant());

        byUpper.Should().NotBeNull();
        byUpper!.Name.Should().Be("Legs");
    }

    [Fact]
    public async Task GetBySlugAsync_returns_null_when_slug_lives_in_another_group()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var owner = await CreateGroupAsync(db, $"owner_{suffix}");
        var stranger = await CreateGroupAsync(db, $"stranger_{suffix}");
        var rule = await CreateRuleAsync(db);

        var slug = $"shared_{suffix}";
        db.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            GroupId = owner.Id,
            XpRuleId = rule.Id,
            Slug = slug,
            Name = "Owned",
        });
        await db.SaveChangesAsync();

        var result = await repo.GetBySlugAsync(stranger.Id, slug);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBySlugAsync_returns_null_when_slug_does_not_exist()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var result = await repo.GetBySlugAsync(GroupIds.Global, $"nope_{suffix}");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_returns_the_category_when_it_exists()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var group = await CreateGroupAsync(db, $"byid_{suffix}");
        var rule = await CreateRuleAsync(db);

        var cat = new Category
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            XpRuleId = rule.Id,
            Slug = $"cat_{suffix}",
            Name = "Cat",
        };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        var result = await repo.GetByIdAsync(cat.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(cat.Id);
        result.GroupId.Should().Be(group.Id);
        result.XpRuleId.Should().Be(rule.Id);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_the_id_is_unknown()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    private static async Task<Group> CreateGroupAsync(AppDbContext db, string name)
    {
        var group = new Group { Id = Guid.NewGuid(), Name = name, IsRoot = false };
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        return group;
    }

    private static async Task<XpRule> CreateRuleAsync(AppDbContext db)
    {
        var rule = new XpRule
        {
            Id = Guid.NewGuid(),
            BaseXp = 10,
            WeightMultiplier = 1.00m,
        };
        db.XpRules.Add(rule);
        await db.SaveChangesAsync();
        return rule;
    }
}
