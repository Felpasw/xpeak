using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Progression.Repositories;
using Xpeak.Api.Tests.Support;

namespace Xpeak.Api.Tests.Progression;

public sealed class XpRuleRepositoryTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public XpRuleRepositoryTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task GetByIdAsync_returns_the_rule_when_it_exists()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<IXpRuleRepository>();

        var rule = new XpRule
        {
            Id = Guid.NewGuid(),
            BaseXp = 15,
            WeightMultiplier = 1.40m,
        };
        db.XpRules.Add(rule);
        await db.SaveChangesAsync();

        var result = await repo.GetByIdAsync(rule.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(rule.Id);
        result.BaseXp.Should().Be(15);
        result.WeightMultiplier.Should().Be(1.40m);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_the_id_is_unknown()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IXpRuleRepository>();

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }
}
