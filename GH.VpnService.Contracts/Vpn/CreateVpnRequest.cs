namespace GH.VpnService.Contracts.Vpn;

public sealed class CreateVpnRequest
{
    public Guid UserId { get; set; }
    public Guid ServerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Days { get; set; } = 30;
}
