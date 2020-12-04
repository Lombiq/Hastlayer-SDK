using Hast.Communication.Models;
using Hast.Communication.Services;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Hast.Catapult.Abstractions.Constants;

namespace Hast.Catapult.Abstractions
{
    public class CatapultCommunicationService : CommunicationServiceBase
    {
        private readonly IDevicePoolPopulator _devicePoolPopulator;
        private readonly IDevicePoolManager _devicePoolManager;
        private readonly ILogger<CatapultLibrary> _catapultLibraryLogger;

        public override string ChannelName => Constants.ChannelName;

        public CatapultCommunicationService(
            IDevicePoolPopulator devicePoolPopulator,
            IDevicePoolManager devicePoolManager,
            ILogger<CatapultCommunicationService> logger,
            ILogger<CatapultLibrary> catapultLibraryLogger) : base(logger)
        {
            _devicePoolPopulator = devicePoolPopulator;
            _devicePoolManager = devicePoolManager;
            _catapultLibraryLogger = catapultLibraryLogger;
        }

        private void Device_Disposing(object sender, EventArgs e) =>
            ((sender as IDevice).Metadata as CatapultLibrary).Dispose();

        #region Temporary solution while the role uses the 16x size hardware cells instead of SimpleMemory cells.
        private const int HardwareCellMultiplier = 16;
        private const int HardwareCellIncrement = HardwareCellMultiplier * SimpleMemory.MemoryCellSizeBytes;

        private Memory<byte> HotfixInput(Memory<byte> memory)
        {
            if (memory.Length <= SimpleMemory.MemoryCellSizeBytes) return memory;
            Memory<byte> hardwareCells = new byte[memory.Length * HardwareCellMultiplier];

            for (int i = 0; i < memory.Length; i += SimpleMemory.MemoryCellSizeBytes)
                memory.Slice(i, SimpleMemory.MemoryCellSizeBytes).CopyTo(hardwareCells.Slice(i * HardwareCellMultiplier));

            return hardwareCells;
        }

        private Memory<byte> HotfixOutput(Memory<byte> memory)
        {
            var memoryBody = memory.Slice(OutputHeaderSizes.Total);
            var softwareCells = memory.Slice(0, OutputHeaderSizes.Total + memoryBody.Length / HardwareCellMultiplier);
            var softwareCellsBody = softwareCells.Slice(OutputHeaderSizes.Total);

            // first one is already at the right place
            for (int i = HardwareCellIncrement; i < memoryBody.Length; i += HardwareCellIncrement)
                memoryBody.Slice(i, SimpleMemory.MemoryCellSizeBytes).CopyTo(softwareCellsBody.Slice(i / HardwareCellMultiplier));

            return softwareCells;
        }
        #endregion

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
                            return CatapultLibrary.Create(config, _catapultLibraryLogger, i);
                        }
                        catch (CatapultFunctionResultException ex)
                        {
                            // The illegal endpoint number messages are normal for higher endpoints if they aren't
                            // populated, so it's OK to suppress them.
                            if (!(i > 0 && ex.Status == Status.IllegalEndpointNumber))
                                Logger.LogError(ex, $"Received {ex.Status} while trying to instantiate CatapultLibrary on EndPoint {i}. This device won't be used.");
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
                lib.WaitClean();
                lib.TesterOutput = TesterOutput;
                var dma = new SimpleMemoryAccessor(simpleMemory);

                // Sending the data.
                var task = lib.AssignJob(memberId, HotfixInput(dma.Get()));
                var outputBuffer = await task;

                // Processing the response.
                var executionTimeClockCycles = MemoryMarshal.Read<ulong>(outputBuffer.Span);
                SetHardwareExecutionTime(context, executionContext, executionTimeClockCycles);

                var outputPayloadByteCount = SimpleMemory.MemoryCellSizeBytes * (int)MemoryMarshal.Read<uint>(
                    outputBuffer.Slice(OutputHeaderSizes.HardwareExecutionTime).Span);
                if (outputBuffer.Length > OutputHeaderSizes.Total + outputPayloadByteCount)
                    outputBuffer = outputBuffer.Slice(0, OutputHeaderSizes.Total + outputPayloadByteCount);

                if (outputPayloadByteCount > SimpleMemory.MemoryCellSizeBytes) outputBuffer = HotfixOutput(outputBuffer);
                dma.Set(outputBuffer, OutputHeaderSizes.Total / SimpleMemory.MemoryCellSizeBytes);
                Logger.LogInformation("Incoming data size in bytes: {0}", outputPayloadByteCount);

                EndExecution(context);

                return context.HardwareExecutionInformation;
            }
        }
    }
}
