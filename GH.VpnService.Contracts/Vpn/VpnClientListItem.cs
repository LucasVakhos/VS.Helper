namespace GH.VpnService.Contracts.Vpn;

public sealed class VpnClientListItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UserLogin { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string AssignedIp { get; set; } = string.Empty;
    public DateTime ExpireAt { get; set; }
    public bool IsEnabled { get; set; }
    public long RxBytes { get; set; }
    public long TxBytes { get; set; }
}
