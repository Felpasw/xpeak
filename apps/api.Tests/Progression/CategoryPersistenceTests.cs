using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Groups;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Tests.Support;

namespace Xpeak.Api.Tests.Progression;

public sealed class CategoryPersistenceTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public CategoryPersistenceTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task Rejects_category_without_a_group()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rule = await CreateRuleAsync(db);
        var suffix = Guid.NewGuid().ToString("N")[..6];

        db.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            GroupId = Guid.Empty,
            XpRuleId = rule.Id,
            Slug = $"orphan_g_{suffix}",
            Name = "Orphan (group)",
        });

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Rejects_category_without_a_rule()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        db.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            GroupId = GroupIds.Global,
            XpRuleId = Guid.Empty,
            Slug = $"orphan_r_{suffix}",
            Name = "Orphan (rule)",
        });

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Allows_same_slug_in_different_groups()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rule = await CreateRuleAsync(db);
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var slug = $"legs_{suffix}";

        db.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            GroupId = GroupIds.Global,
            XpRuleId = rule.Id,
            Slug = slug,
            Name = "Legs (Global)",
        });
        await db.SaveChangesAsync();

        await using var scope2 = _factory.Services.CreateAsyncScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var otherGroup = new Xpeak.Api.Groups.Entities.Group
        {
            Id = Guid.NewGuid(),
            Name = $"Other {suffix}",
            IsRoot = false,
        };
        db2.Groups.Add(otherGroup);
        db2.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            GroupId = otherGroup.Id,
            XpRuleId = rule.Id,
            Slug = slug,
            Name = "Legs (Other)",
        });

        var act = async () => await db2.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Rejects_duplicate_slug_within_the_same_group()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rule = await CreateRuleAsync(db);
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var slug = $"dup_{suffix}";

        db.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            GroupId = GroupIds.Global,
            XpRuleId = rule.Id,
            Slug = slug,
            Name = "First",
        });
        await db.SaveChangesAsync();

        await using var scope2 = _factory.Services.CreateAsyncScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        db2.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            GroupId = GroupIds.Global,
            XpRuleId = rule.Id,
            Slug = slug.ToUpperInvariant(),
            Name = "Second",
        });

        var act = async () => await db2.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Multiple_categories_can_share_the_same_rule()
    {
        // Point of extracting XpRule: two categories with identical
        // scoring (e.g. `chest` and `back`, both 12 XP × 1.2) can point
        // to the same row instead of duplicating the numbers.
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rule = await CreateRuleAsync(db);
        var suffix = Guid.NewGuid().ToString("N")[..6];

        db.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            GroupId = GroupIds.Global,
            XpRuleId = rule.Id,
            Slug = $"a_{suffix}",
            Name = "A",
        });
        db.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            GroupId = GroupIds.Global,
            XpRuleId = rule.Id,
            Slug = $"b_{suffix}",
            Name = "B",
        });

        var act = async () => await db.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    private static async Task<XpRule> CreateRuleAsync(AppDbContext db)
    {
        var rule = new XpRule
        {
            Id = Guid.NewGuid(),
            BaseXp = 12,
            WeightMultiplier = 1.20m,
        };
        db.XpRules.Add(rule);
        await db.SaveChangesAsync();
        return rule;
    }
}
