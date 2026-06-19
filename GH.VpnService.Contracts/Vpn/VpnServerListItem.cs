namespace GH.VpnService.Contracts.Vpn;

public sealed class VpnServerListItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string EndpointPort { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
}
