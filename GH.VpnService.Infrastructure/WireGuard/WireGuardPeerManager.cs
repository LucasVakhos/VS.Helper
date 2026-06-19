using GH.VpnService.Domain.Entities;
using GH.VpnService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GH.VpnService.Infrastructure.WireGuard;

public interface IWireGuardPeerManager
{
    Task<VpnClient> CreateClientAsync(Guid userId, Guid serverId, string name, int days, CancellationToken cancellationToken = default);
    Task<string?> BuildClientConfigAsync(Guid clientId, CancellationToken cancellationToken = default);
    Task<bool> DisableClientAsync(Guid clientId, CancellationToken cancellationToken = default);
    Task<bool> DeleteClientAsync(Guid clientId, CancellationToken cancellationToken = default);
}

public sealed class WireGuardPeerManager(VpnDbContext db, IWireGuardKeyGenerator keyGenerator) : IWireGuardPeerManager
{
    public async Task<VpnClient> CreateClientAsync(Guid userId, Guid serverId, string name, int days, CancellationToken cancellationToken = default)
    {
        var userExists = await db.Users.AnyAsync(x => x.Id == userId && x.IsActive, cancellationToken);
        if (!userExists)
        {
            throw new InvalidOperationException("User not found or inactive.");
        }

        var serverExists = await db.VpnServers.AnyAsync(x => x.Id == serverId, cancellationToken);
        if (!serverExists)
        {
            throw new InvalidOperationException("VPN server not found.");
        }

        var keys = keyGenerator.Generate();
        var assignedIp = await GetNextClientIpAsync(serverId, cancellationToken);

        var client = new VpnClient
        {
            UserId = userId,
            ServerId = serverId,
            Name = string.IsNullOrWhiteSpace(name) ? "Device" : name.Trim(),
            PrivateKey = keys.PrivateKey,
            PublicKey = keys.PublicKey,
            PresharedKey = keys.PresharedKey,
            AssignedIp = assignedIp,
            ExpireAt = DateTime.UtcNow.AddDays(days <= 0 ? 30 : days),
            IsEnabled = true
        };

        db.VpnClients.Add(client);
        await db.SaveChangesAsync(cancellationToken);

        return client;
    }

    public async Task<string?> BuildClientConfigAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var client = await db.VpnClients
            .Include(x => x.Server)
            .FirstOrDefaultAsync(x => x.Id == clientId, cancellationToken);

        if (client?.Server is null)
        {
            return null;
        }

        return BuildClientConfig(client, client.Server);
    }

    public async Task<bool> DisableClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var client = await db.VpnClients.FirstOrDefaultAsync(x => x.Id == clientId, cancellationToken);
        if (client is null)
        {
            return false;
        }

        client.IsEnabled = false;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var client = await db.VpnClients.FirstOrDefaultAsync(x => x.Id == clientId, cancellationToken);
        if (client is null)
        {
            return false;
        }

        db.VpnClients.Remove(client);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<string> GetNextClientIpAsync(Guid serverId, CancellationToken cancellationToken)
    {
        var usedIps = await db.VpnClients
            .Where(x => x.ServerId == serverId)
            .Select(x => x.AssignedIp)
            .ToListAsync(cancellationToken);

        for (var i = 2; i < 255; i++)
        {
            var candidate = $"10.66.66.{i}/32";
            if (!usedIps.Contains(candidate, StringComparer.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No free IP addresses in VPN network.");
    }

    private static string BuildClientConfig(VpnClient client, VpnServer server)
    {
        var endpointPort = string.IsNullOrWhiteSpace(server.EndpointPort) ? "51820" : server.EndpointPort;

        return $"""
[Interface]
PrivateKey = {client.PrivateKey}
Address = {client.AssignedIp}
DNS = 1.1.1.1, 8.8.8.8

[Peer]
PublicKey = {server.PublicKey}
PresharedKey = {client.PresharedKey}
Endpoint = {server.Host}:{endpointPort}
AllowedIPs = 0.0.0.0/0, ::/0
PersistentKeepalive = 25
""";
    }
}
