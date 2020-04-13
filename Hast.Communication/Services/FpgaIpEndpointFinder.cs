using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Hast.Common.Services;
using Hast.Communication.Constants;
using Hast.Communication.Constants.CommunicationConstants;
using Hast.Communication.Helpers;
using Hast.Communication.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hast.Communication.Services
{
    public class FpgaIpEndpointFinder : IFpgaIpEndpointFinder
    {
        private const int AvailabilityCheckerTimeout = 1000;
        private const int BroadcastRetryCount = 2;
        private const string FpgaEndpointsCacheKey = "Hast.Communication.FpgaEndpoints";


        private readonly IClock _clock;


        public ILogger Logger { get; set; }


        public FpgaIpEndpointFinder(IClock clock)
        {
            _clock = clock;

            Logger = NullLogger.Instance;
        }


        public async Task<IEnumerable<IFpgaEndpoint>> FindFpgaEndpoints()
        {
            var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, Ethernet.Ports.WhoIsAvailableRequest);
            var inputBuffer = new[] { (byte)CommandTypes.WhoIsAvailable };

            // We need retries because somehow the FPGA doesn't always catch our request.
            Logger.LogInformation("Starting to find FPGA endpoints. \"Who is available\" request will be sent " + BroadcastRetryCount + 1 + " time(s).");

            var currentRetries = 0;
            var receiveResults = Enumerable.Empty<UdpReceiveResult>();
            while (currentRetries <= BroadcastRetryCount)
            {
                var endpoints = new List<IPEndPoint>();

                // Send request to all broadcast addresses on all the supported network interfaces.
                foreach (var suppertedNetworkInterface in NetworkInterface.GetAllNetworkInterfaces()
                    .Where(networkInterface => networkInterface.OperationalStatus == OperationalStatus.Up && 
                        networkInterface.SupportsMulticast == true))
                {
                    // Currently we are supporting only IPv4 addresses.
                    var ipv4AddressInformations = suppertedNetworkInterface.GetIPProperties().UnicastAddresses
                        .Where(addressInformation => addressInformation.Address.AddressFamily == AddressFamily.InterNetwork);

                    endpoints.AddRange(ipv4AddressInformations.Select(addressInformation => 
                        new IPEndPoint(addressInformation.Address, Ethernet.Ports.WhoIsAvailableResponse)));
                }

                // Sending requests to all the found IP endpoints at the same time.
                var currentReceiveResultLists = await Task.WhenAll(endpoints.Select(endpoint => EthernetCommunicationHelpers
                    .UdpSendAndReceiveAllAsync(inputBuffer, endpoint, broadcastEndpoint, AvailabilityCheckerTimeout)));
                foreach (var currentReceiveResults in currentReceiveResultLists)
                {
                    receiveResults = receiveResults.Union(currentReceiveResults, new UdpReceiveResultEqualityComparer());
                }

                currentRetries++;
            }

            return receiveResults.Select(result => CreateFpgaEndpoint(result.Buffer));
        }


        private FpgaEndpoint CreateFpgaEndpoint(byte[] answerBytes)
        {
            var isAvailable = Convert.ToBoolean(answerBytes[0]);
            var ipAddress = new IPAddress(answerBytes.Skip(1).Take(4).ToArray());
            var port = BitConverter.ToUInt16(answerBytes, 5);

            return new FpgaEndpoint
            {
                IsAvailable = isAvailable,
                Endpoint = new IPEndPoint(ipAddress, port),
                LastCheckedUtc = _clock.UtcNow
            };
        }


        private class UdpReceiveResultEqualityComparer : IEqualityComparer<UdpReceiveResult>
        {
            public bool Equals(UdpReceiveResult firstResult, UdpReceiveResult secondResult)
            {
                return firstResult.RemoteEndPoint.Equals(secondResult.RemoteEndPoint);
            }

            public int GetHashCode(UdpReceiveResult result)
            {
                return result.RemoteEndPoint.GetHashCode();
            }
        }
    }
}
