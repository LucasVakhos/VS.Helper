namespace GH.VpnService.Contracts.Vpn;

public sealed class VpnUserListItem
{
    public Guid Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
