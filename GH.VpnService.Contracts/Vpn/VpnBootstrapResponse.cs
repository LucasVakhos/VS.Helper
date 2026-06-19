namespace GH.VpnService.Contracts.Vpn;

public sealed class VpnBootstrapResponse
{
    public List<VpnUserListItem> Users { get; set; } = [];
    public List<VpnServerListItem> Servers { get; set; } = [];
}
