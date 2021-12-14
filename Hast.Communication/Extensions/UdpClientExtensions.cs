using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    internal static class UdpClientExtensions
    {
        /// <summary>
        /// Receives an UDP datagram if there is one in present or wait one until the timeout expires. 
        /// In that case a default object will be returned.
        /// </summary>
        /// <param name="receiveTimeoutMilliseconds">Timeout within the client needs to wait for an UDP datagram.</param>
        /// <returns>Returns the datagram if received or a default value if the timeout has expired.</returns>
        public static async Task<UdpReceiveResult> ReceiveAsync(this UdpClient client, int receiveTimeoutMilliseconds)
        {
            var receiveTask = client.ReceiveAsync();

            if (await Task.WhenAny(receiveTask, Task.Delay(receiveTimeoutMilliseconds)) == receiveTask)
                return await receiveTask;

            return default(UdpReceiveResult);
        }

        /// <summary>
        /// Receives all the UDP datagrams coming simultaneously. 
        /// Stops receiving when the timeout has expired after the last datagram.
        /// </summary>
        /// <param name="receiveTimeoutMilliseconds">Timeout within the client needs to wait for a single datagram.</param>
        /// <returns>Returns the datagram list.</returns>
        public static async Task<IEnumerable<UdpReceiveResult>> ReceiveAllAsync(this UdpClient client, int receiveTimeoutMilliseconds)
        {
            var datagramList = new List<UdpReceiveResult>();

            var read = true;
            while (read)
            {
                var result = await client.ReceiveAsync(receiveTimeoutMilliseconds);

                if (result != default(UdpReceiveResult)) datagramList.Add(result);
                else read = false;
            }

            return datagramList;
        }
    }
}
