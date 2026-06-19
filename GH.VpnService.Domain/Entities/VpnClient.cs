namespace GH.VpnService.Domain.Entities;

public class VpnClient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid ServerId { get; set; }
    public VpnServer? Server { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string PresharedKey { get; set; } = string.Empty;
    public string AssignedIp { get; set; } = string.Empty;
    public DateTime ExpireAt { get; set; }
    public bool IsEnabled { get; set; } = true;
    public long RxBytes { get; set; }
    public long TxBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}