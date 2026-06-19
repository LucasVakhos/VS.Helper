using System.Security.Cryptography;

namespace GH.VpnService.Infrastructure.WireGuard;

public interface IWireGuardKeyGenerator
{
    WireGuardKeys Generate();
    string GenerateKey();
}

public sealed class WireGuardKeyGenerator : IWireGuardKeyGenerator
{
    public WireGuardKeys Generate()
    {
        var privateKey = GenerateKey();
        var publicKey = GenerateKey();
        var presharedKey = GenerateKey();

        return new WireGuardKeys(privateKey, publicKey, presharedKey);
    }

    public string GenerateKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
