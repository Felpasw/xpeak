using Microsoft.EntityFrameworkCore;

namespace Xpeak.Api.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    // DbSet<TEntity> lines land here as we add domain models.
    // Example (once we have users):
    // public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Configuration will be moved to per-entity config classes as models grow.
    }
}
