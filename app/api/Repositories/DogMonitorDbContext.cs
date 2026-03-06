using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

public sealed class DogMonitorDbContext(DbContextOptions<DogMonitorDbContext> options) : DbContext(options)
{
    internal DbSet<SiteStateRecord> SiteStates => Set<SiteStateRecord>();
    internal DbSet<DogRecord> Dogs => Set<DogRecord>();
    internal DbSet<PushSubscriptionRecord> PushSubscriptions => Set<PushSubscriptionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SiteStateRecord>(e =>
        {
            e.ToTable("SiteState");
            e.HasKey(r => r.Id);
        });

        modelBuilder.Entity<DogRecord>(e =>
        {
            e.ToTable("Dogs");
            e.HasKey(r => r.Aid);
        });

        modelBuilder.Entity<PushSubscriptionRecord>(e =>
        {
            e.ToTable("PushSubscriptions");
            e.HasKey(r => r.EndpointHash);
        });
    }
}
