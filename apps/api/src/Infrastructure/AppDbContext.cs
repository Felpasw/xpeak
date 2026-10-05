using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Groups;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Media.Entities;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Users;

namespace Xpeak.Api.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower),
        },
    };

    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    public DbSet<Group> Groups => Set<Group>();

    public DbSet<GroupMembership> GroupMemberships => Set<GroupMembership>();

    public DbSet<GroupConfig> GroupConfigs => Set<GroupConfig>();

    public DbSet<XpRule> XpRules => Set<XpRule>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<CheckIn> CheckIns => Set<CheckIn>();

    public DbSet<CheckInMedia> CheckInMedia => Set<CheckInMedia>();

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
            b.Property(u => u.TimeZone)
                .HasColumnName("time_zone")
                .HasMaxLength(64)
                .HasDefaultValue("America/Sao_Paulo");
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

            b.HasData(new XpRule
            {
                Id = SeedIds.DefaultXpRule,
                BaseXp = 10,
                WeightMultiplier = 1.0m,
                CreatedAt = SeedIds.SeedTimestamp,
                UpdatedAt = SeedIds.SeedTimestamp,
            });
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

            b.HasData(SeedIds.GlobalCategories);
        });

        modelBuilder.Entity<GroupConfig>(b =>
        {
            b.ToTable("group_configs");
            b.HasKey(gc => gc.GroupId);
            b.Property(gc => gc.GroupId).HasColumnName("group_id");
            b.Property(gc => gc.StreakConfig)
                .HasColumnName("streak_config")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOpts),
                    v => JsonSerializer.Deserialize<StreakConfig>(v, JsonOpts)!);
            b.Property(gc => gc.CreatedAt).HasColumnName("created_at");
            b.Property(gc => gc.UpdatedAt).HasColumnName("updated_at");

            b.HasOne<Group>()
                .WithOne()
                .HasForeignKey<GroupConfig>(gc => gc.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasData(new GroupConfig
            {
                GroupId = GroupIds.Global,
                StreakConfig = new StreakConfig(StreakMode.Daily),
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                UpdatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            });
        });

        modelBuilder.Entity<CheckIn>(b =>
        {
            b.ToTable("check_ins", t =>
            {
                // No CHECK on xp_earned: it's server-written, never from
                // user input, and the "pending media upload" state uses
                // the natural value 0 (flipped to a positive total on
                // publish). The old `> 0` CHECK was defensive against a
                // scenario the code never produces.
                t.HasCheckConstraint(
                    "ck_check_ins_duration_positive",
                    "duration_minutes IS NULL OR duration_minutes > 0");
                t.HasCheckConstraint(
                    "ck_check_ins_notes_length",
                    "notes IS NULL OR length(notes) <= 280");
                t.HasCheckConstraint(
                    "ck_check_ins_title_shape",
                    "length(trim(title)) > 0 AND length(title) <= 60");
            });

            b.HasKey(c => c.Id);
            b.Property(c => c.Id).HasColumnName("id");
            b.Property(c => c.UserId).HasColumnName("user_id");
            b.Property(c => c.CategoryId).HasColumnName("category_id");
            b.Property(c => c.GroupId).HasColumnName("group_id");
            b.Property(c => c.Title).HasColumnName("title").HasMaxLength(60).IsRequired();
            b.Property(c => c.XpEarned).HasColumnName("xp_earned");
            b.Property(c => c.ScoringSnapshot)
                .HasColumnName("scoring_snapshot")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOpts),
                    v => JsonSerializer.Deserialize<ScoringSnapshot>(v, JsonOpts)!);
            b.Property(c => c.PerformedAt).HasColumnName("performed_at");
            b.Property(c => c.DurationMinutes).HasColumnName("duration_minutes");
            b.Property(c => c.Notes).HasColumnName("notes");
            b.Property(c => c.CreatedAt).HasColumnName("created_at");
            b.Property(c => c.UpdatedAt).HasColumnName("updated_at");

            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<Category>()
                .WithMany()
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Group>()
                .WithMany()
                .HasForeignKey(c => c.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(c => new { c.UserId, c.PerformedAt })
                .HasDatabaseName("ix_check_ins_user_performed_desc")
                .IsDescending(false, true);
            b.HasIndex(c => new { c.UserId, c.GroupId, c.PerformedAt })
                .HasDatabaseName("ix_check_ins_user_group_performed_desc")
                .IsDescending(false, false, true);
            b.HasIndex(c => new { c.GroupId, c.PerformedAt })
                .HasDatabaseName("ix_check_ins_group_performed_desc")
                .IsDescending(false, true);
        });

        modelBuilder.Entity<CheckInMedia>(b =>
        {
            b.ToTable("check_in_media", t =>
            {
                t.HasCheckConstraint(
                    "ck_check_in_media_kind",
                    "kind IN ('photo', 'video')");
                t.HasCheckConstraint(
                    "ck_check_in_media_position_non_negative",
                    "position >= 0");
                t.HasCheckConstraint(
                    "ck_check_in_media_storage_key_not_blank",
                    "length(trim(storage_key)) > 0");
                t.HasCheckConstraint(
                    "ck_check_in_media_dimensions_non_negative",
                    "(width IS NULL OR width > 0) AND (height IS NULL OR height > 0)");
                t.HasCheckConstraint(
                    "ck_check_in_media_duration_non_negative",
                    "duration_seconds IS NULL OR duration_seconds > 0");
            });

            b.HasKey(m => m.Id);
            b.Property(m => m.Id).HasColumnName("id");
            b.Property(m => m.CheckInId).HasColumnName("check_in_id");
            b.Property(m => m.Kind)
                .HasColumnName("kind")
                .HasMaxLength(16)
                .HasConversion(
                    v => v.ToString().ToLowerInvariant(),
                    v => Enum.Parse<MediaKind>(v, true));
            b.Property(m => m.StorageKey).HasColumnName("storage_key");
            b.Property(m => m.Width).HasColumnName("width");
            b.Property(m => m.Height).HasColumnName("height");
            b.Property(m => m.DurationSeconds).HasColumnName("duration_seconds");
            b.Property(m => m.Position).HasColumnName("position").HasDefaultValue(0);
            b.Property(m => m.CreatedAt).HasColumnName("created_at");
            b.Property(m => m.UpdatedAt).HasColumnName("updated_at");

            b.HasOne<CheckIn>()
                .WithMany()
                .HasForeignKey(m => m.CheckInId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(m => m.StorageKey)
                .IsUnique()
                .HasDatabaseName("ux_check_in_media_storage_key");
            b.HasIndex(m => new { m.CheckInId, m.Position })
                .HasDatabaseName("ix_check_in_media_check_in_position");
        });
    }
}
