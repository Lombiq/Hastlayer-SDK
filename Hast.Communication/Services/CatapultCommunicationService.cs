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
                var device = await Task.Run(() =>
                {
                    try
                    {
                        // add configuration for manifest location and logging?
                        return new CatapultLibrary();
                    }
                    catch (CatapultFunctionResultException ex)
                    {
                        Logger.Error(ex, $"Received {ex.Status} while trying to instantiate CatapultLibrary.");
                        return null;
                    }
                });
                return device is null ? new Device[0] :
                    new[] { new Device { Identifier = Catapult.ChannelName, Metadata = device } };
            });

            using (var device = await _devicePoolManager.ReserveDevice())
            {
                var context = BeginExecution();
                CatapultLibrary lib = device.Metadata;

                // This actually happens inside lib.ExecuteJob.
                if (simpleMemory.Memory.Length < Catapult.BufferMessageSizeMin)
                    Logger.Warning("Incoming data is {0}B! Padding with zeroes to reach the minimum of {1}B...",
                        simpleMemory.Memory.Length, Catapult.BufferMessageSizeMin);

                // Sending the data.
                var outputBuffer = await lib.ExecuteJob(memberId, simpleMemory.Memory);

                // Processing the response.

                // TODO get execution time somehow?
                // SetHardwareExecutionTime(context, executionContext, executionTimeClockCycles:)

                Logger.Information("Incoming data size in bytes: {0}", outputBuffer.Length);

                simpleMemory.Memory = outputBuffer;

                EndExecution(context);

                return context.HardwareExecutionInformation;
            }
        }
    }
}
