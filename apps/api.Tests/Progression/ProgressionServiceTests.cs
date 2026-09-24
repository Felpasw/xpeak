using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Progression.Services;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Xp;

namespace Xpeak.Api.Tests.Progression;

public sealed class ProgressionServiceTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public ProgressionServiceTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task Resolves_from_DI_and_delegates_category_reads_and_arithmetic()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IProgressionService>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = $"svc_{suffix}",
            IsRoot = false,
        };
        var rule = new XpRule
        {
            Id = Guid.NewGuid(),
            BaseXp = 15,
            WeightMultiplier = 1.40m,
        };
        var category = new Category
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            XpRuleId = rule.Id,
            Slug = $"legs_{suffix}",
            Name = "Legs",
        };
        db.Groups.Add(group);
        db.XpRules.Add(rule);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var listed = await service.ListActiveCategoriesAsync(group.Id);
        var found = await service.GetCategoryBySlugAsync(group.Id, category.Slug);

        listed.Should().ContainSingle().Which.Id.Should().Be(category.Id);
        found.Should().NotBeNull();
        found!.Id.Should().Be(category.Id);

        service.ComputeXp(rule).Should().Be(21);
        service.XpForLevel(10).Should().Be(3_162);
        service.LevelForXp(3_162).Should().Be(10);
        service.EvaluateLevelUp(99, 100)
            .Should().Be(new LevelUpResult(true, 1, 1));
    }
}
