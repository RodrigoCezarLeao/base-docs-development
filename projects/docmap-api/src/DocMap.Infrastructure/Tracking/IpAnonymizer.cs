using System.Net;
using System.Net.Sockets;

namespace DocMap.Infrastructure.Tracking;

/// <summary>Optionally masks IPs (config Tracking:AnonymizeIp): last IPv4 octet / IPv6 /64 prefix.</summary>
public sealed class IpAnonymizer(bool enabled)
{
    public string Apply(IPAddress? ip)
    {
        if (ip is null) return "unknown";
        if (!enabled) return ip.ToString();

        var bytes = ip.GetAddressBytes();
        if (ip.AddressFamily == AddressFamily.InterNetwork && bytes.Length == 4)
        {
            bytes[3] = 0;
            return new IPAddress(bytes).ToString();
        }
        if (ip.AddressFamily == AddressFamily.InterNetworkV6 && bytes.Length == 16)
        {
            for (var i = 8; i < 16; i++) bytes[i] = 0; // keep the /64 prefix
            return new IPAddress(bytes).ToString();
        }
        return ip.ToString();
    }
}
