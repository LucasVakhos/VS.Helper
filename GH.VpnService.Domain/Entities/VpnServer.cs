namespace GH.VpnService.Domain.Entities;

public class VpnServer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string EndpointPort { get; set; } = "51820";
    public string NetworkCidr { get; set; } = "10.66.66.0/24";
    public bool IsOnline { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<VpnClient> Clients { get; set; } = new List<VpnClient>();
}