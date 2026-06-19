namespace GH.VpnService.Infrastructure.WireGuard;

public sealed record WireGuardKeys(string PrivateKey, string PublicKey, string PresharedKey);
