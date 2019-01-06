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
        private const int InputMemoryPrefixCellCount = 1;


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

                var dma = new SimpleMemoryAccessor(simpleMemory);
                // Get input data, add member id as prefix. schema: (int memberId, byte[] data)
                var memory = dma.Get(InputMemoryPrefixCellCount);
                MemoryMarshal.Write(memory.Span, ref memberId);

                // This actually happens inside lib.ExecuteJob.
                int memoryLength = simpleMemory.CellCount * SimpleMemory.MemoryCellSizeBytes;
                if (memoryLength < Constants.BufferMessageSizeMinByte)
                    Logger.Warning("Incoming data is {0}B! Padding with zeros to reach the minimum of {1}B...",
                        memoryLength, Constants.BufferMessageSizeMinByte);
                else if (memoryLength % 16 != 0)
                    Logger.Warning("Incoming data ({0}B) must be aligned to 16B! Padding for {1}B...",
                        memoryLength, 16 - (memoryLength % 16));

                // Sending the data.
                var outputBuffer = await lib.ExecuteJob(memory);

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
