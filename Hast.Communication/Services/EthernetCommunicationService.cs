using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Hast.Communication.Constants;
using Hast.Communication.Constants.CommunicationConstants;
using Hast.Communication.Exceptions;
using Hast.Communication.Models;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using Orchard.Logging;

namespace Hast.Communication.Services
{
    public class EthernetCommunicationService : CommunicationServiceBase
    {
        private const int TcpConnectionTimeout = 3000;
        // This has to be maximum the number set for the TCP MSS in the Hastlayer hardware project.
        private const int ReceiveBufferSize = 1460;


        private readonly IDevicePoolPopulator _devicePoolPopulator;
        private readonly IDevicePoolManager _devicePoolManager;
        private readonly IFpgaIpEndpointFinder _fpgaIpEndpointFinder;


        public override string ChannelName
        {
            get
            {
                return Ethernet.ChannelName;
            }
        }


        public EthernetCommunicationService(
            IDevicePoolPopulator devicePoolPopulator,
            IDevicePoolManager devicePoolManager,
            IFpgaIpEndpointFinder fpgaIpEndpointFinder)
        {
            _devicePoolPopulator = devicePoolPopulator;
            _devicePoolManager = devicePoolManager;
            _fpgaIpEndpointFinder = fpgaIpEndpointFinder;
        }


        public override async Task<IHardwareExecutionInformation> Execute(
            SimpleMemory simpleMemory,
            int memberId,
            IHardwareExecutionContext executionContext)
        {
            _devicePoolPopulator.PopulateDevicePoolIfNew(async () =>
                {
                    // Get the IP addresses of the FPGA boards.
                    var fpgaEndpoints = await _fpgaIpEndpointFinder.FindFpgaEndpoints();

                    if (!fpgaEndpoints.Any())
                    {
                        throw new EthernetCommunicationException("Couldn't find any FPGAs on the network.");
                    }

                    return fpgaEndpoints.Select(endpoint =>
                        new Device { Identifier = endpoint.Endpoint.Address.ToString(), Metadata = endpoint });
                });


            using (var device = await _devicePoolManager.ReserveDevice())
            {
                var context = BeginExecution();

                IFpgaEndpoint fpgaEndpoint = device.Metadata;
                var fpgaIpEndpoint = fpgaEndpoint.Endpoint;

                Logger.Information("IP endpoint to communicate with via Ethernet: {0}:{1}", fpgaIpEndpoint.Address, fpgaIpEndpoint.Port);

                try
                {
                    using (var client = new TcpClient())
                    {
                        // Initialize the connection.
                        if (!await client.ConnectAsync(fpgaIpEndpoint, TcpConnectionTimeout))
                        {
                            throw new EthernetCommunicationException("Couldn't connect to FPGA before the timeout exceeded.");
                        }

                        using (var stream = client.GetStream())
                        {
                            // We send an execution signal to make the FPGA ready to receive the data stream.
                            var executionCommandTypeByte = new byte[] { (byte)CommandTypes.Execution };
                            stream.Write(executionCommandTypeByte, 0, executionCommandTypeByte.Length);

                            var executionCommandTypeResponseByte = await GetBytesFromStream(stream, 1);

                            if (executionCommandTypeResponseByte[0] != Ethernet.Signals.Ready)
                            {
                                throw new EthernetCommunicationException("Awaited a ready signal from the FPGA after the execution byte was sent but received the following byte instead: " + executionCommandTypeResponseByte[0]);
                            }

                            // Here we put together the data stream.
                            var dma = new DirectSimpleMemoryAccess(simpleMemory);
                            var lengthBytes = BitConverter.GetBytes(dma.Read().Length);
                            var memberIdBytes = BitConverter.GetBytes(memberId);

                            // Copying the input length, represented as bytes, to the output buffer.
                            var outputBuffer = BitConverter.GetBytes(dma.Read().Length)
                                // Copying the member ID, represented as bytes, to the output buffer.
                                .Append(BitConverter.GetBytes(memberId))
                                // Copying the simple memory.
                                .Append(dma.Read());

                            // Sending data to the FPGA board.
                            stream.Write(outputBuffer, 0, outputBuffer.Length);


                            // Read the first batch of the TcpServer response bytes that will represent the execution time.
                            var executionTimeBytes = await GetBytesFromStream(stream, 8);

                            var executionTimeClockCycles = BitConverter.ToUInt64(executionTimeBytes, 0);

                            SetHardwareExecutionTime(context, executionContext, executionTimeClockCycles);

                            // Read the bytes representing the length of the simple memory.
                            var outputByteCountBytes = await GetBytesFromStream(stream, 4);

                            var outputByteCount = BitConverter.ToUInt32(outputByteCountBytes, 0);

                            Logger.Information("Incoming data size in bytes: {0}", outputByteCount);

                            // Finally read the memory itself.
                            var outputBytes = await GetBytesFromStream(stream, (int)outputByteCount);

                            dma.Write(outputBytes);
                        }
                    }
                }
                catch (SocketException ex)
                {
                    throw new EthernetCommunicationException("An unexpected error occurred during the Ethernet communication.", ex);
                }

                EndExecution(context);

                return context.HardwareExecutionInformation;
            }
        }


        public static async Task<byte[]> GetBytesFromStream(NetworkStream stream, int length)
        {
            var outputBytes = new byte[length];

            var readPosition = 0;
            var remaining = length;
            while (readPosition < length)
            {
                readPosition += await stream.ReadAsync(outputBytes, readPosition, remaining > ReceiveBufferSize ? ReceiveBufferSize : remaining);
                remaining = length - readPosition;
            }

            return outputBytes;
        }
    }
}
