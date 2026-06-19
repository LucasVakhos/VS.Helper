namespace GH.VpnService.Win.Settings;

public sealed class WinClientSettings
{
    public string ApiBaseUrl { get; set; } = "https://localhost:7226";
    public Guid? LastUserId { get; set; }
    public Guid? LastServerId { get; set; }
    public string LastClientName { get; set; } = Environment.MachineName;
    public int Days { get; set; } = 30;
    public bool MinimizeToTray { get; set; } = true;
    public TunnelMode TunnelMode { get; set; } = TunnelMode.Full;
    public List<string> AllowedDomains { get; set; } = new()
    {
        "github.com",
        "openai.com",
        "stihi.ru"
    };
}
