using GH.VpnService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GH.VpnService.Infrastructure.Persistence;

public class VpnDbContext(DbContextOptions<VpnDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<VpnServer> VpnServers => Set<VpnServer>();
    public DbSet<VpnClient> VpnClients => Set<VpnClient>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}