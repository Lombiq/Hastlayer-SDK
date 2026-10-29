using System.Threading.Tasks;

namespace System.Net.Sockets;

internal static class TcpClientExtensions
{
    /// <summary>
    /// Connects the client to a remote TCP host using the specified IP endpoint as an asynchronous operation with a
    /// timeout.
    /// </summary>
    /// <param name="endpoint">Endpoint where the client needs to connect to.</param>
    /// <param name="connectionTimeoutMilliseconds">
    /// Timeout within the client needs to connect to the remote TCP host.
    /// </param>
    /// <returns>Returns <see langword="true"/> if the connection was established before the timeout exceeded.</returns>
    public static async Task<bool> ConnectAsync(this TcpClient client, IPEndPoint endpoint, int connectionTimeoutMilliseconds)
    {
        var connectTask = client.ConnectAsync(endpoint.Address, endpoint.Port);

        return await Task.WhenAny(connectTask, Task.Delay(connectionTimeoutMilliseconds)) == connectTask;
    }
}
