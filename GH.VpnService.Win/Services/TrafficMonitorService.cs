using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GH.VpnService.Win.Services;

public sealed class TrafficMonitorService
{
    private readonly WireGuardWindowsService _wireGuard;
    private long _lastRx;
    private long _lastTx;
    private DateTimeOffset? _lastCheckedAt;
    private DateTimeOffset? _connectedSince;

    public TrafficMonitorService(WireGuardWindowsService wireGuard)
    {
        _wireGuard = wireGuard;
    }

    public async Task<TrafficSnapshot> GetSnapshotAsync(string tunnelName, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.Now;
        var status = await _wireGuard.GetStatusAsync(tunnelName, cancellationToken);

        var rx = 0L;
        var tx = 0L;

        if (status.Equals("Running", StringComparison.OrdinalIgnoreCase))
        {
            _connectedSince ??= now;
            var adapterName = "wg-" + tunnelName;
            (rx, tx) = TryReadNetworkInterfaceBytes(adapterName);
        }
        else
        {
            _connectedSince = null;
        }

        var seconds = _lastCheckedAt is null
            ? 0
            : Math.Max(0.001, (now - _lastCheckedAt.Value).TotalSeconds);

        var down = _lastCheckedAt is null ? 0 : Math.Max(0, (rx - _lastRx) / seconds);
        var up = _lastCheckedAt is null ? 0 : Math.Max(0, (tx - _lastTx) / seconds);

        _lastRx = rx;
        _lastTx = tx;
        _lastCheckedAt = now;

        return new TrafficSnapshot(
            tunnelName,
            status,
            rx,
            tx,
            down,
            up,
            _connectedSince is null ? TimeSpan.Zero : now - _connectedSince.Value,
            now);
    }

    private static (long rx, long tx) TryReadNetworkInterfaceBytes(string adapterName)
    {
        try
        {
            var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            var iface = interfaces.FirstOrDefault(x =>
                x.Name.Equals(adapterName, StringComparison.OrdinalIgnoreCase) ||
                x.Description.Contains(adapterName, StringComparison.OrdinalIgnoreCase) ||
                x.Name.Contains("WireGuard", StringComparison.OrdinalIgnoreCase) ||
                x.Description.Contains("WireGuard", StringComparison.OrdinalIgnoreCase));

            if (iface is null)
                return (0, 0);

            var stats = iface.GetIPv4Statistics();
            return (stats.BytesReceived, stats.BytesSent);
        }
        catch
        {
            return (0, 0);
        }
    }
}
