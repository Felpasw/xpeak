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

    [Fact]
    public async Task IsMemberAsync_returns_true_after_the_user_joins_the_group()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var groups = scope.ServiceProvider.GetRequiredService<IGroupService>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"mem_{suffix}", Email = $"mem_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();
        await groups.AddUserToGlobalAsync(user.Id);

        var isMember = await groups.IsMemberAsync(user.Id, GroupIds.Global);

        isMember.Should().BeTrue();
    }

    [Fact]
    public async Task IsMemberAsync_returns_false_when_the_user_is_not_in_the_group()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var groups = scope.ServiceProvider.GetRequiredService<IGroupService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"out_{suffix}", Email = $"out_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();

        var other = new Xpeak.Api.Groups.Entities.Group
        {
            Id = Guid.NewGuid(),
            Name = $"private_{suffix}",
            IsRoot = false,
        };
        db.Groups.Add(other);
        await db.SaveChangesAsync();

        var isMember = await groups.IsMemberAsync(user.Id, other.Id);

        isMember.Should().BeFalse();
    }

    [Fact]
    public async Task IsMemberAsync_returns_false_for_unknown_ids()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var groups = scope.ServiceProvider.GetRequiredService<IGroupService>();

        var isMember = await groups.IsMemberAsync(Guid.NewGuid(), Guid.NewGuid());

        isMember.Should().BeFalse();
    }
}
