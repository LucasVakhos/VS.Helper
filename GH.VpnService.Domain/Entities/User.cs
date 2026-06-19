namespace GH.VpnService.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Login { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<VpnClient> VpnClients { get; set; } = new List<VpnClient>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}