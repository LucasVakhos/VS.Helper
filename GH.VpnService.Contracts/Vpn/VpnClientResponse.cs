namespace GH.VpnService.Contracts.Vpn;

public sealed class VpnClientResponse
{
    public Guid ClientId { get; set; }
    public Guid UserId { get; set; }
    public Guid ServerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AssignedIp { get; set; } = string.Empty;
    public DateTime ExpireAt { get; set; }
    public bool IsEnabled { get; set; }
    public string Config { get; set; } = string.Empty;
}
