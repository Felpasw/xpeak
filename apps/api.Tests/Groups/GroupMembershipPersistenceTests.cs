using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Groups;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Groups;

public sealed class GroupMembershipPersistenceTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public GroupMembershipPersistenceTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task Composite_primary_key_prevents_duplicate_memberships()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"mem_{suffix}", Email = $"mem_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();

        db.GroupMemberships.Add(new GroupMembership
        {
            UserId = user.Id,
            GroupId = GroupIds.Global,
            JoinedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        await using var scope2 = _factory.Services.CreateAsyncScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        db2.GroupMemberships.Add(new GroupMembership
        {
            UserId = user.Id,
            GroupId = GroupIds.Global,
            JoinedAt = DateTimeOffset.UtcNow,
        });

        var act = async () => await db2.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Cascades_membership_deletion_when_user_is_deleted()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"casc_{suffix}", Email = $"casc_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();

        db.GroupMemberships.Add(new GroupMembership
        {
            UserId = user.Id,
            GroupId = GroupIds.Global,
            JoinedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        (await users.DeleteAsync(user)).Succeeded.Should().BeTrue();

        var remains = await db.GroupMemberships
            .AsNoTracking()
            .AnyAsync(m => m.UserId == user.Id);
        remains.Should().BeFalse();
    }
}
