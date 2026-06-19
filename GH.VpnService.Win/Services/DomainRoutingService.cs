using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace GH.VpnService.Win.Services;

public sealed class DomainRoutingService
{
    private static readonly Regex DomainRegex = new(
        @"^(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,63}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<SelectedSitesRoute> BuildSelectedSitesConfigAsync(string configText, IEnumerable<string> domains, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(configText))
            throw new InvalidOperationException("WireGuard config is empty.");

        var normalizedDomains = NormalizeDomains(domains);
        if (normalizedDomains.Count == 0)
            throw new InvalidOperationException("Добавьте хотя бы один домен для режима 'Только сайты'.");

        var allowedIps = await ResolveAllowedIpsAsync(normalizedDomains, cancellationToken);
        if (allowedIps.Count == 0)
            throw new InvalidOperationException("Не удалось получить IP для списка доменов. Проверьте список сайтов и интернет-соединение.");

        var newConfig = ReplaceAllowedIps(configText, string.Join(", ", allowedIps));
        return new SelectedSitesRoute(newConfig, normalizedDomains, allowedIps);
    }

    public async Task<IReadOnlyList<string>> ResolveAllowedIpsAsync(IEnumerable<string> domains, CancellationToken cancellationToken = default)
    {
        var result = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var domain in NormalizeDomains(domains))
        {
            cancellationToken.ThrowIfCancellationRequested();

            IPAddress[] addresses;
            try
            {
                addresses = await Dns.GetHostAddressesAsync(domain, cancellationToken);
            }
            catch (SocketException)
            {
                continue;
            }

            foreach (var ip in addresses)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    result.Add($"{ip}/32");
                else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                    result.Add($"{ip}/128");
            }
        }

        return result.ToList();
    }

    public static IReadOnlyList<string> NormalizeDomains(IEnumerable<string> domains)
    {
        return domains
            .SelectMany(x => (x ?? string.Empty).Split(new[] { '\r', '\n', ',', ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            .Select(x => x.Trim().TrimEnd('.').ToLowerInvariant())
            .Select(StripUrlNoise)
            .Where(x => DomainRegex.IsMatch(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<string> ApplySelectedSitesAsync(string configText, IEnumerable<string> domains, CancellationToken cancellationToken = default)
    {
        return (await BuildSelectedSitesConfigAsync(configText, domains, cancellationToken)).ConfigText;
    }

    private static string StripUrlNoise(string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
            return uri.Host.TrimEnd('.').ToLowerInvariant();

        var slashIndex = value.IndexOf('/');
        if (slashIndex >= 0)
            value = value[..slashIndex];

        var colonIndex = value.IndexOf(':');
        if (colonIndex >= 0)
            value = value[..colonIndex];

        return value.TrimEnd('.').ToLowerInvariant();
    }

    private static string ReplaceAllowedIps(string configText, string allowedIps)
    {
        var lines = configText.Replace("\r\n", "\n").Split('\n').ToList();
        var inPeer = false;
        var replaced = false;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Trim();
            if (line.Equals("[Peer]", StringComparison.OrdinalIgnoreCase))
            {
                inPeer = true;
                continue;
            }

            if (line.StartsWith("[", StringComparison.Ordinal) && !line.Equals("[Peer]", StringComparison.OrdinalIgnoreCase))
                inPeer = false;

            if (inPeer && line.StartsWith("AllowedIPs", StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = $"AllowedIPs = {allowedIps}";
                replaced = true;
                break;
            }
        }

        if (!replaced)
            throw new InvalidOperationException("В WireGuard config не найден параметр AllowedIPs в секции [Peer].");

        return string.Join(Environment.NewLine, lines);
    }
}

public sealed record SelectedSitesRoute(string ConfigText, IReadOnlyList<string> Domains, IReadOnlyList<string> AllowedIps);
