using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Hast.Communication.Constants;
using Hast.Communication.Exceptions;
using Hast.Communication.Models;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using Orchard.Logging;

using Catapult = Hast.Communication.Constants.CommunicationConstants.Catapult;

namespace Hast.Communication.Services
{
    public class CatapultCommunicationService : CommunicationServiceBase
    {
        private readonly IDevicePoolPopulator _devicePoolPopulator;
        private readonly IDevicePoolManager _devicePoolManager;


        public override string ChannelName =>
            CommunicationConstants.Catapult.ChannelName;


        public CatapultCommunicationService(
            IDevicePoolPopulator devicePoolPopulator,
            IDevicePoolManager devicePoolManager)
        {
            _devicePoolPopulator = devicePoolPopulator;
            _devicePoolManager = devicePoolManager;
        }


        public override async Task<IHardwareExecutionInformation> Execute(
            SimpleMemory simpleMemory,
            int memberId,
            IHardwareExecutionContext executionContext)
        {
            _devicePoolPopulator.PopulateDevicePoolIfNew(async () =>
            {
                // TODO set up devices
                return new IDevice[0];
            });

            using (var device = await _devicePoolManager.ReserveDevice())
            {
                var context = BeginExecution();


                //using (var serialPort = CreateSerialPort(executionContext))
                {
                    // TODO configure device
                    

                    // Here we put together the data stream.

                    // Execute Order 66.
                    var outputBuffer = new byte[] { (byte)CommandTypes.Execution }
                        // Copying the input length, represented as bytes, to the output buffer.
                        .Append(BitConverter.GetBytes(simpleMemory.Memory.Length))
                        // Copying the member ID, represented as bytes, to the output buffer.
                        .Append(BitConverter.GetBytes(memberId))
                        // Copying the simple memory.
                        .Append(simpleMemory.Memory);

                    // Sending the data.
                    // Just using serialPort.Write() once with all the data would stop sending data after 16372 bytes so
                    // we need to create batches. Since the FPGA receives data in the multiples of 4 bytes we use a batch
                    // of 4 bytes. This seems to have no negative impact on performance compared to using
                    // serialPort.Write() once.
                    var maxBytesToSendAtOnce = 4;
                    for (int i = 0; i < (int)Math.Ceiling(outputBuffer.Length / (decimal)maxBytesToSendAtOnce); i++)
                    {
                        // TODO send out data
                    }


                    // Processing the response.

                    // In this event we are receiving the useful data coming from the FPGA board.

                    //await taskCompletionSource.Task;

                    EndExecution(context);

                    return context.HardwareExecutionInformation;
                }
            }
        }
    }
}
