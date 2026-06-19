using System.Diagnostics;
using System.Security.Principal;

namespace GH.VpnService.Win.Services;

public sealed class KillSwitchService
{
    private const string Group = "GH VPN Kill Switch";

    public async Task EnableAsync(CancellationToken cancellationToken = default)
    {
        EnsureAdministrator();

        await DisableAsync(cancellationToken);

        // Блокируем обычный исходящий интернет, но оставляем WireGuard и loopback.
        await NetshAsync("advfirewall firewall add rule name=\"GH VPN Kill Switch - Block Out\" dir=out action=block enable=yes profile=any group=\"" + Group + "\"", cancellationToken);
        await NetshAsync("advfirewall firewall add rule name=\"GH VPN Kill Switch - Allow WireGuard\" dir=out action=allow program=\"%ProgramFiles%\\WireGuard\\wireguard.exe\" enable=yes profile=any group=\"" + Group + "\"", cancellationToken);
        await NetshAsync("advfirewall firewall add rule name=\"GH VPN Kill Switch - Allow Localhost\" dir=out action=allow remoteip=127.0.0.1,::1 enable=yes profile=any group=\"" + Group + "\"", cancellationToken);
    }

    public async Task DisableAsync(CancellationToken cancellationToken = default)
    {
        await NetshAsync("advfirewall firewall delete rule group=\"" + Group + "\"", cancellationToken, ignoreErrors: true);
    }

    public async Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
    {
        var result = await RunProcessAsync("netsh.exe", "advfirewall firewall show rule name=\"GH VPN Kill Switch - Block Out\"", cancellationToken);
        return result.ExitCode == 0 && result.Output.Contains("GH VPN Kill Switch - Block Out", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task NetshAsync(string arguments, CancellationToken cancellationToken, bool ignoreErrors = false)
    {
        var result = await RunProcessAsync("netsh.exe", arguments, cancellationToken);
        if (!ignoreErrors && result.ExitCode != 0)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.Error) ? result.Output : result.Error);
    }

    private static void EnsureAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);

        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            throw new InvalidOperationException("Run GH.VpnService.Win as Administrator to enable Kill Switch.");
    }

    private static async Task<ProcessResult> RunProcessAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Cannot start {fileName}.");
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return new ProcessResult(process.ExitCode, await outputTask, await errorTask);
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}
