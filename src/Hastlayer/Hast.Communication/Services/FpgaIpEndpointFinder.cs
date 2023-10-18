using Hast.Common.Services;
using Hast.Communication.Constants;
using Hast.Communication.Constants.CommunicationConstants;
using Hast.Communication.Helpers;
using Hast.Communication.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Hast.Communication.Services;

public class FpgaIpEndpointFinder : IFpgaIpEndpointFinder
{
    private const int AvailabilityCheckerTimeout = 1_000;
    private const int BroadcastRetryCount = 2;

    private readonly IClock _clock;
    private readonly ILogger _logger;

    public FpgaIpEndpointFinder(IClock clock, ILogger<FpgaIpEndpointFinder> logger)
    {
        _clock = clock;
        _logger = logger;
    }

    [SuppressMessage(
        "Style",
        "IDE0230:Use UTF-8 string literal",
        Justification = "False positive, shouldn't replace the char constant with a literal.")]
    public async Task<IEnumerable<IFpgaEndpoint>> FindFpgaEndpointsAsync()
    {
        var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, Ethernet.Ports.WhoIsAvailableRequest);

        // We need retries because somehow the FPGA doesn't always catch our request.
        _logger.LogInformation(
            "Starting to find FPGA endpoints. \"Who is available\" request will be sent {BroadcastCount} time(s).",
            BroadcastRetryCount + 1);

        var currentRetries = 0;
        var receiveResults = Enumerable.Empty<UdpReceiveResult>();
        while (currentRetries <= BroadcastRetryCount)
        {
            var endpoints = new List<IPEndPoint>();

            // Send request to all broadcast addresses on all the supported network interfaces.
            foreach (var supportedNetworkInterface in NetworkInterface.GetAllNetworkInterfaces()
                .Where(networkInterface => networkInterface.OperationalStatus == OperationalStatus.Up &&
                    networkInterface.SupportsMulticast))
            {
                // Currently we are supporting only IPv4 addresses.
                var ipv4AddressInformation = supportedNetworkInterface.GetIPProperties().UnicastAddresses
                    .Where(addressInformation => addressInformation.Address.AddressFamily == AddressFamily.InterNetwork);

                endpoints.AddRange(ipv4AddressInformation.Select(addressInformation =>
                    new IPEndPoint(addressInformation.Address, Ethernet.Ports.WhoIsAvailableResponse)));
            }

            // Sending requests to all the found IP endpoints at the same time.
            var currentReceiveResultLists = await Task.WhenAll(endpoints.Select(endpoint => EthernetCommunicationHelpers
                .UdpSendAndReceiveAllAsync(CommandTypes.WhoIsAvailable, endpoint, broadcastEndpoint, AvailabilityCheckerTimeout)));
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
            LastCheckedUtc = _clock.UtcNow,
        };
    }

    private sealed class UdpReceiveResultEqualityComparer : IEqualityComparer<UdpReceiveResult>
    {
        public bool Equals(UdpReceiveResult x, UdpReceiveResult y) => x.RemoteEndPoint.Equals(y.RemoteEndPoint);

        public int GetHashCode(UdpReceiveResult obj) => obj.RemoteEndPoint.GetHashCode();
    }
}
