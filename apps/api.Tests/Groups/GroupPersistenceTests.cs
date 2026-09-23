using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Groups;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;

namespace Xpeak.Api.Tests.Groups;

public sealed class GroupPersistenceTests : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public GroupPersistenceTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task Seeds_the_global_root_group_on_migration()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var global = await db.Groups
            .AsNoTracking()
            .SingleAsync(g => g.Id == GroupIds.Global);

        global.Name.Should().Be("Global");
        global.IsRoot.Should().BeTrue();
    }

    [Fact]
    public async Task Only_one_group_is_marked_as_root()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var rootCount = await db.Groups.AsNoTracking().CountAsync(g => g.IsRoot);

        rootCount.Should().Be(1);
    }
}
