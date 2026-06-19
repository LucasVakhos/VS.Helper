namespace GH.VpnService.Win.Services;

public sealed class AutoReconnectService
{
    private readonly WireGuardWindowsService _wireGuard;
    private bool _isReconnecting;

    public AutoReconnectService(WireGuardWindowsService wireGuard)
    {
        _wireGuard = wireGuard;
    }

    public async Task<bool> EnsureConnectedAsync(string tunnelName, string configText, CancellationToken cancellationToken = default)
    {
        if (_isReconnecting || string.IsNullOrWhiteSpace(configText))
            return false;

        var status = await _wireGuard.GetStatusAsync(tunnelName, cancellationToken);
        if (status.Equals("Running", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            _isReconnecting = true;
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            await _wireGuard.ConnectAsync(tunnelName, configText, cancellationToken);
            return true;
        }
        finally
        {
            _isReconnecting = false;
        }
    }
}
