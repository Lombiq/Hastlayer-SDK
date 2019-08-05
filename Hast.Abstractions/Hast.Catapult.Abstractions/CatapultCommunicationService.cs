using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Hast.Communication.Models;
using Hast.Communication.Services;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using Orchard.Logging;
using static Hast.Catapult.Abstractions.Constants;

namespace Hast.Catapult.Abstractions
{
    public class CatapultCommunicationService : CommunicationServiceBase
    {
        private readonly IDevicePoolPopulator _devicePoolPopulator;
        private readonly IDevicePoolManager _devicePoolManager;


        public override string ChannelName => Constants.ChannelName;


        public CatapultCommunicationService(
            IDevicePoolPopulator devicePoolPopulator,
            IDevicePoolManager devicePoolManager)
        {
            _devicePoolPopulator = devicePoolPopulator;
            _devicePoolManager = devicePoolManager;
        }

        private void Device_Disposing(object sender, EventArgs e) => 
            ((sender as IDevice).Metadata as CatapultLibrary).Dispose();

        public override async Task<IHardwareExecutionInformation> Execute(
            SimpleMemory simpleMemory,
            int memberId,
            IHardwareExecutionContext executionContext)
        {
            _devicePoolPopulator.PopulateDevicePoolIfNew(async () =>
            {
                // Because the FPGA_GetNumberEndpoints function is not implemented in the current driver (and it's not
                // included in the CatapultNativeLibrary interface because of that) it's not possible to know the
                // number of endpoints. Instead this algorithm probes the first 8 indices. The single device is
                // expected to be in endpoint 0 according to spec so this will get at least one result always.
                var libraries = await Task.WhenAll(Enumerable.Range(0, 7).Select(i => Task.Run(() =>
                    {
                        try
                        {
                            var config = executionContext.ProxyGenerationConfiguration.CustomConfiguration;
                            return CatapultLibrary.Create(config, Logger, i);
                        }
                        catch (CatapultFunctionResultException ex)
                        {
                            Logger.Error(ex, $"Received {ex.Status} while trying to instantiate CatapultLibrary on EndPoint {i}. This device won't be used.");
                            return null;
                        }
                    })));
                
                return libraries
                    .Where(x => x != null)
                    .Select(x => new Device(x.InstanceName, x, Device_Disposing));
            });

            using (var device = await _devicePoolManager.ReserveDevice())
            {
                var context = BeginExecution();
                CatapultLibrary lib = device.Metadata;
                lib.TesterOutput = TesterOutput;
                var dma = new SimpleMemoryAccessor(simpleMemory);

                // Sending the data.
                var task = lib.AssignJob(memberId, dma.Get());
                var outputBuffer = await task;

                // Processing the response.
                var executionTimeClockCycles = MemoryMarshal.Read<ulong>(outputBuffer.Span);
                SetHardwareExecutionTime(context, executionContext, executionTimeClockCycles);

                var outputPayloadByteCount = SimpleMemory.MemoryCellSizeBytes * (int)MemoryMarshal.Read<uint>(
                    outputBuffer.Slice(OutputHeaderSizes.HardwareExecutionTime).Span);
                if (outputBuffer.Length > OutputHeaderSizes.Total + outputPayloadByteCount)
                    outputBuffer = outputBuffer.Slice(0, OutputHeaderSizes.Total + outputPayloadByteCount);

                dma.Set(outputBuffer, Constants.OutputHeaderSizes.Total / SimpleMemory.MemoryCellSizeBytes);
                Logger.Information("Incoming data size in bytes: {0}", outputPayloadByteCount);

                EndExecution(context);

                return context.HardwareExecutionInformation;
            }
        }
    }
}
