using System.Net;
using System.Net.Sockets;

namespace Toolbelt.Blazor.I18nText.Test.Internals;

internal static class Network
{
    internal static int GetAvailableTCPPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
