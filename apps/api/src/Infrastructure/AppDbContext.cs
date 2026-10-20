using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.Challenges.Entities;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Media.Entities;
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

    public DbSet<CheckIn> CheckIns => Set<CheckIn>();

    public DbSet<CheckInMedia> CheckInMedia => Set<CheckInMedia>();

    public DbSet<Challenge> Challenges => Set<Challenge>();

    public DbSet<ChallengeMembership> ChallengeMemberships => Set<ChallengeMembership>();

    public DbSet<ChallengeInvitation> ChallengeInvitations => Set<ChallengeInvitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("citext");

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

        modelBuilder.Entity<CheckIn>(b =>
        {
            b.ToTable("check_ins", t =>
            {
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

            b.HasIndex(c => new { c.UserId, c.PerformedAt })
                .HasDatabaseName("ix_check_ins_user_performed_desc")
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

        modelBuilder.Entity<Challenge>(b =>
        {
            b.ToTable("challenges", t =>
            {
                t.HasCheckConstraint(
                    "ck_challenges_visibility",
                    "visibility IN ('public', 'private')");
                t.HasCheckConstraint(
                    "ck_challenges_state",
                    "state IN ('active', 'completed', 'failed')");
                t.HasCheckConstraint(
                    "ck_challenges_xp_multiplier_range",
                    "xp_multiplier >= 0.5 AND xp_multiplier <= 1.5");
                t.HasCheckConstraint(
                    "ck_challenges_window",
                    "starts_at < ends_at");
                t.HasCheckConstraint(
                    "ck_challenges_title_shape",
                    "length(trim(title)) > 0 AND length(title) <= 80");
                t.HasCheckConstraint(
                    "ck_challenges_member_cap_positive",
                    "member_cap IS NULL OR member_cap > 0");
                t.HasCheckConstraint(
                    "ck_challenges_target_level_positive",
                    "target_level > 0");
            });

            b.HasKey(c => c.Id);
            b.Property(c => c.Id).HasColumnName("id");
            b.Property(c => c.OwnerId).HasColumnName("owner_id");
            b.Property(c => c.Title).HasColumnName("title").HasMaxLength(80).IsRequired();
            b.Property(c => c.Description).HasColumnName("description");
            b.Property(c => c.IconPublicId).HasColumnName("icon_public_id");
            b.Property(c => c.Visibility)
                .HasColumnName("visibility")
                .HasMaxLength(16)
                .HasConversion(
                    v => v.ToString().ToLowerInvariant(),
                    v => Enum.Parse<ChallengeVisibility>(v, true));
            b.Property(c => c.TargetLevel).HasColumnName("target_level");
            b.Property(c => c.XpMultiplier)
                .HasColumnName("xp_multiplier")
                .HasColumnType("numeric(4,2)")
                .HasDefaultValue(1.0m);
            b.Property(c => c.StartsAt).HasColumnName("starts_at");
            b.Property(c => c.EndsAt).HasColumnName("ends_at");
            b.Property(c => c.MemberCap).HasColumnName("member_cap");
            b.Property(c => c.State)
                .HasColumnName("state")
                .HasMaxLength(16)
                .HasConversion(
                    v => v.ToString().ToLowerInvariant(),
                    v => Enum.Parse<ChallengeState>(v, true));
            b.Property(c => c.CreatedAt).HasColumnName("created_at");
            b.Property(c => c.UpdatedAt).HasColumnName("updated_at");

            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(c => c.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(c => c.OwnerId).HasDatabaseName("ix_challenges_owner_id");
            b.HasIndex(c => c.State).HasDatabaseName("ix_challenges_state");
            b.HasIndex(c => c.Visibility).HasDatabaseName("ix_challenges_visibility");
        });

        modelBuilder.Entity<ChallengeMembership>(b =>
        {
            b.ToTable("challenge_memberships", t =>
            {
                t.HasCheckConstraint(
                    "ck_challenge_memberships_role",
                    "role IN ('owner', 'member')");
                t.HasCheckConstraint(
                    "ck_challenge_memberships_result",
                    "result IS NULL OR result IN ('completed', 'failed', 'cancelled')");
            });

            b.HasKey(m => m.Id);
            b.Property(m => m.Id).HasColumnName("id");
            b.Property(m => m.ChallengeId).HasColumnName("challenge_id");
            b.Property(m => m.UserId).HasColumnName("user_id");
            b.Property(m => m.Role)
                .HasColumnName("role")
                .HasMaxLength(16)
                .HasConversion(
                    v => v.ToString().ToLowerInvariant(),
                    v => Enum.Parse<ChallengeMembershipRole>(v, true));
            b.Property(m => m.JoinedAt).HasColumnName("joined_at");
            b.Property(m => m.LeftAt).HasColumnName("left_at");
            b.Property(m => m.Progress)
                .HasColumnName("progress")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOpts),
                    v => JsonSerializer.Deserialize<MembershipProgress>(v, JsonOpts)!);
            b.Property(m => m.Result)
                .HasColumnName("result")
                .HasMaxLength(16)
                .HasConversion(
                    v => v == null ? null : v.ToString()!.ToLowerInvariant(),
                    v => v == null ? null : Enum.Parse<ChallengeMembershipResult>(v, true));

            b.HasOne<Challenge>()
                .WithMany()
                .HasForeignKey(m => m.ChallengeId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(m => new { m.ChallengeId, m.UserId })
                .IsUnique()
                .HasDatabaseName("ux_challenge_memberships_challenge_user");
        });

        modelBuilder.Entity<ChallengeInvitation>(b =>
        {
            b.ToTable("challenge_invitations", t =>
            {
                t.HasCheckConstraint(
                    "ck_challenge_invitations_state",
                    "state IN ('pending', 'accepted', 'rejected', 'expired')");
            });

            b.HasKey(i => i.Id);
            b.Property(i => i.Id).HasColumnName("id");
            b.Property(i => i.ChallengeId).HasColumnName("challenge_id");
            b.Property(i => i.InvitedByUserId).HasColumnName("invited_by_user_id");
            b.Property(i => i.InviteeUserId).HasColumnName("invitee_user_id");
            b.Property(i => i.State)
                .HasColumnName("state")
                .HasMaxLength(16)
                .HasConversion(
                    v => v.ToString().ToLowerInvariant(),
                    v => Enum.Parse<ChallengeInvitationState>(v, true));
            b.Property(i => i.CreatedAt).HasColumnName("created_at");
            b.Property(i => i.UpdatedAt).HasColumnName("updated_at");

            b.HasOne<Challenge>()
                .WithMany()
                .HasForeignKey(i => i.ChallengeId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(i => i.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(i => i.InviteeUserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(i => new { i.ChallengeId, i.InviteeUserId })
                .HasDatabaseName("ix_challenge_invitations_challenge_invitee");
            b.HasIndex(i => new { i.ChallengeId, i.State })
                .HasDatabaseName("ix_challenge_invitations_challenge_state");
        });
    }
}
