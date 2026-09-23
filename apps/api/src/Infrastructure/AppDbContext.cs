using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.Users;

namespace Xpeak.Api.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
    }
}
