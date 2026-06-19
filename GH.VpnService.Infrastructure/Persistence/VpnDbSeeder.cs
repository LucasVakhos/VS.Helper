using GH.VpnService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GH.VpnService.Infrastructure.Persistence;

public static class VpnDbSeeder
{
    public static async Task SeedAsync(VpnDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        if (!await db.VpnServers.AnyAsync(cancellationToken))
        {
            db.VpnServers.Add(new VpnServer
            {
                Name = "Amsterdam-1",
                Host = "vpn1.example.com",
                Country = "Netherlands",
                PublicKey = "SERVER_PUBLIC_KEY",
                EndpointPort = "51820",
                NetworkCidr = "10.66.66.0/24",
                IsOnline = true
            });
        }

        if (!await db.Users.AnyAsync(cancellationToken))
        {
            db.Users.Add(new User
            {
                Login = "admin",
                Email = "admin@vpn.local",
                PasswordHash = "admin",
                IsActive = true
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
