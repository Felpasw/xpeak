using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.Groups;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Users;

namespace Xpeak.Api.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    public DbSet<Group> Groups => Set<Group>();

    public DbSet<GroupMembership> GroupMemberships => Set<GroupMembership>();

    public DbSet<XpRule> XpRules => Set<XpRule>();

    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("citext");

        // Rename Identity tables to snake_case (PostgreSQL convention)
        // and drop the ASP.NET-style "AspNet" prefix.
        modelBuilder.Entity<AppUser>(b =>
        {
            b.ToTable("users");
            b.Property(u => u.AvatarUrl).HasColumnName("avatar_url");
            b.Property(u => u.GoogleUid).HasColumnName("google_uid").HasMaxLength(255);
            b.Property(u => u.Level).HasColumnName("level").HasDefaultValue(0);
            b.Property(u => u.Xp).HasColumnName("xp").HasDefaultValue(0);
            b.Property(u => u.CreatedAt).HasColumnName("created_at");

            b.HasIndex(u => u.GoogleUid).IsUnique().HasFilter("google_uid IS NOT NULL");
        });

        modelBuilder.Entity<IdentityRole<Guid>>(b => b.ToTable("roles"));
        modelBuilder.Entity<IdentityUserRole<Guid>>(b => b.ToTable("user_roles"));
        modelBuilder.Entity<IdentityUserClaim<Guid>>(b => b.ToTable("user_claims"));
        modelBuilder.Entity<IdentityUserLogin<Guid>>(b => b.ToTable("user_logins"));
        modelBuilder.Entity<IdentityRoleClaim<Guid>>(b => b.ToTable("role_claims"));
        modelBuilder.Entity<IdentityUserToken<Guid>>(b => b.ToTable("user_tokens"));

        modelBuilder.Entity<RevokedToken>(b =>
        {
            b.ToTable("revoked_tokens");
            b.HasKey(t => t.Jti);
            b.Property(t => t.Jti).HasColumnName("jti").HasMaxLength(64);
            b.Property(t => t.UserId).HasColumnName("user_id");
            b.Property(t => t.ExpiresAt).HasColumnName("expires_at");
            b.Property(t => t.RevokedAt).HasColumnName("revoked_at");
            b.HasIndex(t => t.ExpiresAt);
        });

        modelBuilder.Entity<Group>(b =>
        {
            b.ToTable("groups");
            b.HasKey(g => g.Id);
            b.Property(g => g.Id).HasColumnName("id");
            b.Property(g => g.Name).HasColumnName("name").IsRequired();
            b.Property(g => g.IsRoot).HasColumnName("is_root").HasDefaultValue(false);
            b.Property(g => g.CreatedAt).HasColumnName("created_at");
            b.Property(g => g.UpdatedAt).HasColumnName("updated_at");

            b.HasData(new Group
            {
                Id = GroupIds.Global,
                Name = "Global",
                IsRoot = true,
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            });
        });

        modelBuilder.Entity<GroupMembership>(b =>
        {
            b.ToTable("group_memberships");
            b.HasKey(m => new { m.UserId, m.GroupId });
            b.Property(m => m.UserId).HasColumnName("user_id");
            b.Property(m => m.GroupId).HasColumnName("group_id");
            b.Property(m => m.JoinedAt).HasColumnName("joined_at");

            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<Group>()
                .WithMany()
                .HasForeignKey(m => m.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(m => m.GroupId);
        });

        modelBuilder.Entity<XpRule>(b =>
        {
            b.ToTable("xp_rules", t =>
            {
                t.HasCheckConstraint("ck_xp_rules_base_xp_positive", "base_xp > 0");
                t.HasCheckConstraint(
                    "ck_xp_rules_weight_multiplier_range",
                    "weight_multiplier >= 0.5 AND weight_multiplier <= 2.5");
            });

            b.HasKey(r => r.Id);
            b.Property(r => r.Id).HasColumnName("id");
            b.Property(r => r.BaseXp).HasColumnName("base_xp");
            b.Property(r => r.WeightMultiplier)
                .HasColumnName("weight_multiplier")
                .HasColumnType("numeric(4,2)");
            b.Property(r => r.CreatedAt).HasColumnName("created_at");
            b.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Category>(b =>
        {
            b.ToTable("categories");
            b.HasKey(c => c.Id);
            b.Property(c => c.Id).HasColumnName("id");
            b.Property(c => c.GroupId).HasColumnName("group_id");
            b.Property(c => c.XpRuleId).HasColumnName("xp_rule_id");
            b.Property(c => c.Slug).HasColumnName("slug").HasColumnType("citext").IsRequired();
            b.Property(c => c.Name).HasColumnName("name").IsRequired();
            b.Property(c => c.IconPublicId).HasColumnName("icon_public_id");
            b.Property(c => c.Active).HasColumnName("active").HasDefaultValue(true);
            b.Property(c => c.CreatedAt).HasColumnName("created_at");
            b.Property(c => c.UpdatedAt).HasColumnName("updated_at");

            b.HasOne<Group>()
                .WithMany()
                .HasForeignKey(c => c.GroupId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne<XpRule>()
                .WithMany()
                .HasForeignKey(c => c.XpRuleId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(c => new { c.GroupId, c.Slug }).IsUnique();
        });
    }
}
