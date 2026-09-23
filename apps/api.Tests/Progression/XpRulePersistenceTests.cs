using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Tests.Support;

namespace Xpeak.Api.Tests.Progression;

public sealed class XpRulePersistenceTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public XpRulePersistenceTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task Rejects_weight_multiplier_below_lower_bound()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.XpRules.Add(new XpRule
        {
            Id = Guid.NewGuid(),
            BaseXp = 10,
            WeightMultiplier = 0.4m,
        });

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Rejects_weight_multiplier_above_upper_bound()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.XpRules.Add(new XpRule
        {
            Id = Guid.NewGuid(),
            BaseXp = 10,
            WeightMultiplier = 2.6m,
        });

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Rejects_negative_base_xp()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.XpRules.Add(new XpRule
        {
            Id = Guid.NewGuid(),
            BaseXp = -1,
            WeightMultiplier = 1.0m,
        });

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
