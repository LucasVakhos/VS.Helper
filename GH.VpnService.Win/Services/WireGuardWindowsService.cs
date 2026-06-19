using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace GH.VpnService.Win.Services;

public sealed class WireGuardWindowsService
{
    private const string TunnelPrefix = "WireGuardTunnel$";

    public string AppFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GH.VpnService.Win");

    public string ConfigFolder => Path.Combine(AppFolder, "configs");

    public string WireGuardExePath { get; }

    public WireGuardWindowsService()
    {
        WireGuardExePath = FindWireGuardExePath();
        Directory.CreateDirectory(ConfigFolder);
    }

    public bool IsWireGuardInstalled => File.Exists(WireGuardExePath);

    public string SaveConfig(string tunnelName, string configText)
    {
        if (string.IsNullOrWhiteSpace(configText))
            throw new InvalidOperationException("WireGuard config is empty.");

        var safeName = MakeSafeTunnelName(tunnelName);
        var filePath = Path.Combine(ConfigFolder, safeName + ".conf");

        File.WriteAllText(filePath, configText, Encoding.UTF8);
        return filePath;
    }

    public async Task ConnectAsync(string tunnelName, string configText, CancellationToken cancellationToken = default)
    {
        EnsureInstalled();

        var safeName = MakeSafeTunnelName(tunnelName);
        await DisconnectAsync(safeName, ignoreMissing: true, cancellationToken);

        var configPath = SaveConfig(safeName, configText);
        await RunProcessAsync(WireGuardExePath, $"/installtunnelservice \"{configPath}\"", true, cancellationToken);
    }

    public async Task DisconnectAsync(string tunnelName, CancellationToken cancellationToken = default)
    {
        await DisconnectAsync(tunnelName, ignoreMissing: false, cancellationToken);
    }

    public async Task<string> GetStatusAsync(string tunnelName, CancellationToken cancellationToken = default)
    {
        var safeName = MakeSafeTunnelName(tunnelName);
        var serviceName = TunnelPrefix + safeName;

        var result = await RunProcessAsync("sc.exe", $"query \"{serviceName}\"", false, cancellationToken);
        if (result.ExitCode != 0)
            return "Not installed";

        if (result.Output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase))
            return "Running";

        if (result.Output.Contains("STOPPED", StringComparison.OrdinalIgnoreCase))
            return "Stopped";

        return "Installed";
    }

    public string GetDefaultTunnelName(string? clientName)
    {
        if (string.IsNullOrWhiteSpace(clientName))
            return "gh-vpn";

        return MakeSafeTunnelName(clientName);
    }

    private async Task DisconnectAsync(string tunnelName, bool ignoreMissing, CancellationToken cancellationToken)
    {
        EnsureInstalled();

        var safeName = MakeSafeTunnelName(tunnelName);
        var result = await RunProcessAsync(WireGuardExePath, $"/uninstalltunnelservice \"{safeName}\"", false, cancellationToken);

        if (result.ExitCode != 0 && !ignoreMissing)
            throw new InvalidOperationException(result.GetErrorMessage());
    }

    private void EnsureInstalled()
    {
        if (!IsRunningAsAdministrator())
        {
            throw new InvalidOperationException("Run GH.VpnService.Win as Administrator to install or remove WireGuard tunnels.");
        }

        if (!IsWireGuardInstalled)
        {
            throw new FileNotFoundException(
                "WireGuard for Windows is not installed. Install it from wireguard.com/install/ and try again.",
                WireGuardExePath);
        }
    }

    private static bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static string FindWireGuardExePath()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WireGuard", "wireguard.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "WireGuard", "wireguard.exe"),
            "wireguard.exe"
        };

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    private static string MakeSafeTunnelName(string value)
    {
        var name = Regex.Replace(value.Trim(), "[^a-zA-Z0-9_.-]+", "-");
        name = name.Trim('-', '.', '_');
        return string.IsNullOrWhiteSpace(name) ? "gh-vpn" : name;
    }

    private static async Task<ProcessResult> RunProcessAsync(string fileName, string arguments, bool throwOnError, CancellationToken cancellationToken)
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

        var result = new ProcessResult(
            process.ExitCode,
            await outputTask,
            await errorTask);

        if (throwOnError && result.ExitCode != 0)
            throw new InvalidOperationException(result.GetErrorMessage());

        return result;
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error)
    {
        public string GetErrorMessage()
        {
            var message = string.Join(Environment.NewLine, new[] { Error, Output }.Where(x => !string.IsNullOrWhiteSpace(x)));
            return string.IsNullOrWhiteSpace(message)
                ? $"Process exited with code {ExitCode}."
                : message;
        }
    }
}
