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
                // included in the CatapultNativeLibrary interface because of that) it's not possible to know the number
                // of endpoints. Instead this algorithm probes the first 8 indices. The single device is expected to be in
                // endpoint 0 according to spec so this will get at least one result always.
                var bag = new ConcurrentBag<CatapultLibrary>();
                await Task.WhenAll(Enumerable.Range(0, 7).Select(i => Task.Run(() =>
                    {
                        try
                        {
                            var config = executionContext.ProxyGenerationConfiguration.CustomConfiguration;
                            bag.Add(CatapultLibrary.Create(config, Logger, i));
                        }
                        catch (CatapultFunctionResultException ex)
                        {
                            Logger.Error(ex, $"Received {ex.Status} while trying to instantiate CatapultLibrary on EndPoint {i}.");
                        }
                    })));

                var libraries = bag.ToArray();
                return libraries
                    .Select(x => new Device(Constants.ChannelName, x, Device_Disposing))
                    .ToArray();
            });

            using (var device = await _devicePoolManager.ReserveDevice())
            {
                var context = BeginExecution();
                CatapultLibrary lib = device.Metadata;

                int memoryLength = simpleMemory.CellCount * SimpleMemory.MemoryCellSizeBytes;
                var dma = new SimpleMemoryAccessor(simpleMemory);
                // Get input data, add member id as prefix. schema: (int memberId, byte[] data)
                var memory = dma.Get();
                MemoryMarshal.Write(memory.Span, ref memberId);
                MemoryMarshal.Write(memory.Slice(sizeof(int)).Span, ref memoryLength);

                // Sending the data.
                var task = lib.AssignJob(memory);
                var outputBuffer = await task;

                // Processing the response. schema: (ulong time, uint length, byte[] data)

                var executionTimeClockCycles = MemoryMarshal.Read<ulong>(outputBuffer.Span);
                SetHardwareExecutionTime(context, executionContext, executionTimeClockCycles);

                // TODO uncomment this when the FPGA side implements the above response format
                var outputByteCount = outputBuffer.Length; //MemoryMarshal.Read<uint>(outputBuffer.Slice(sizeof(ulong)).Span);
                //outputBuffer = outputBuffer.Slice(0, sizeof(ulong) + sizeof(uint) + (int)outputByteCount);

                dma.Set(outputBuffer, (sizeof(ulong) + sizeof(uint)) / SimpleMemory.MemoryCellSizeBytes);
                Logger.Information("Incoming data size in bytes: {0}", outputByteCount);

                EndExecution(context);

                return context.HardwareExecutionInformation;
            }
        }
    }
}
