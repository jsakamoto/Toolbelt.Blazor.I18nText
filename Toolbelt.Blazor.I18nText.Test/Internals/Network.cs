using System.Net.NetworkInformation;

namespace Toolbelt.Blazor.I18nText.Test.Internals;

internal static class Network
{
    /// <summary>
    /// Gets a free TCP port number.
    /// </summary>
    /// <returns>A free TCP port number.</returns>
    internal static int GetAvailableTCPPort() => EnumCandidatePorts().First(port => GetUsedTcpPorts().All(usedPort => usedPort != port));

    /// <summary>
    /// Gets the list of used TCP ports.
    /// </summary>
    /// <returns>An enumerable of used TCP port numbers.</returns>
    private static IEnumerable<int> GetUsedTcpPorts() => IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections().Select(tcp => tcp.LocalEndPoint.Port);

    /// <summary>
    /// Enumerates candidate TCP port numbers.
    /// </summary>
    /// <returns>An enumerable of candidate TCP port numbers.</returns>
    private static IEnumerable<int> EnumCandidatePorts()
    {
        for (; ; ) { yield return Random.Shared.Next(6000, 7000); }
    }
}
