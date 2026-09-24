using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Groups;
using Xpeak.Api.Groups.Services;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Groups;

public sealed class GroupServiceTests : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public GroupServiceTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task AddUserToGlobalAsync_inserts_the_membership()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var groups = scope.ServiceProvider.GetRequiredService<IGroupService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"svc_{suffix}", Email = $"svc_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();

        await groups.AddUserToGlobalAsync(user.Id);

        var membership = await db.GroupMemberships
            .AsNoTracking()
            .SingleAsync(m => m.UserId == user.Id && m.GroupId == GroupIds.Global);
        membership.Should().NotBeNull();
    }

    [Fact]
    public async Task AddUserToGlobalAsync_is_idempotent_on_repeat_calls()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var groups = scope.ServiceProvider.GetRequiredService<IGroupService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"idem_{suffix}", Email = $"idem_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();

        await groups.AddUserToGlobalAsync(user.Id);
        await groups.AddUserToGlobalAsync(user.Id);
        await groups.AddUserToGlobalAsync(user.Id);

        var count = await db.GroupMemberships
            .AsNoTracking()
            .CountAsync(m => m.UserId == user.Id && m.GroupId == GroupIds.Global);
        count.Should().Be(1);
    }
}
