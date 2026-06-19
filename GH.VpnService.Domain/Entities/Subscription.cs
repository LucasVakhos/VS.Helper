namespace GH.VpnService.Domain.Entities;

public class Subscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public decimal Price { get; set; }
    public int Days { get; set; }
    public DateTime StartAt { get; set; } = DateTime.UtcNow;
    public DateTime EndAt { get; set; }
    public bool Paid { get; set; }
}