using Hast.Communication.Models;
using Hast.Communication.Services;
using Hast.Layer;
using Hast.Vitis.Abstractions.Models;
using Hast.Transformer.Abstractions.SimpleMemory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Hast.Transformer.Abstractions.SimpleMemory.SimpleMemory;

namespace Hast.Vitis.Abstractions.Services
{
    public abstract class OpenClCommunicationService : CommunicationServiceBase
    {
        public const string KernelName = "hastip";


        private readonly IDevicePoolPopulator _devicePoolPopulator;
        private readonly IDevicePoolManager _devicePoolManager;

        private readonly IBinaryOpenCl _binaryOpenCl;
        private readonly IOpenClConfiguration _configuration;


        protected OpenClCommunicationService(
            IDevicePoolPopulator devicePoolPopulator,
            IDevicePoolManager devicePoolManager,
            IBinaryOpenCl binaryOpenCl,
            IOpenClConfiguration configuration,
            ILogger logger) : base(logger)
        {
            _devicePoolPopulator = devicePoolPopulator;
            _devicePoolManager = devicePoolManager;
            _binaryOpenCl = binaryOpenCl;
            _configuration = configuration;
        }


        public override async Task<IHardwareExecutionInformation> Execute(
            SimpleMemory simpleMemory,
            int memberId,
            IHardwareExecutionContext executionContext)
        {
            var kernelBinary = File.ReadAllBytes(_configuration.BinaryFilePath);
            var kernelName = KernelName;
            _binaryOpenCl.CreateBinaryKernel(kernelBinary, kernelName);

            _devicePoolPopulator.PopulateDevicePoolIfNew(() =>
            {
                var devices = new List<IDevice>(_binaryOpenCl.DeviceCount);
                for (var i = 0; i < _binaryOpenCl.DeviceCount; i++)
                {
                    devices.Add(new Device
                    {
                        Identifier = $"{ChannelName}:{_configuration.VendorName ?? "any"}:{i}",
                        Metadata = i
                    });
                    _binaryOpenCl.CreateCommandQueue(i);
                }

                return Task.FromResult<IEnumerable<IDevice>>(devices);
            });

            using (var device = await _devicePoolManager.ReserveDevice())
            {
                var context = BeginExecution();
                {
                    int deviceIndex = device.Metadata;
                    var memoryAccessor = new SimpleMemoryAccessor(simpleMemory);

                    // Prepare host buffer.
                    var hostMemory = memoryAccessor.Get(_configuration.HeaderCellCount);
                    hostMemory.Span.SetIntegers(0, _configuration.HeaderCellCount, memberId);

                    using (var hostMemoryHandle = hostMemory.Pin())
                    {
                        // Send data and execute.
                        var fpgaBuffer = _binaryOpenCl.SetKernelArgumentWithNewBuffer(kernelName, 0, hostMemoryHandle, hostMemory.Length);
                        Logger.LogInformation("KERNEL #{0} ARGUMENT SET", 0);
                        _binaryOpenCl.LaunchKernel(deviceIndex, kernelName, new[] {fpgaBuffer});
                        await _binaryOpenCl.AwaitDevice(deviceIndex);
                        var resultMetadata = GetResultMetadata(memoryAccessor);

                        // Read out metadata.
                        SetHardwareExecutionTime(context, executionContext, resultMetadata.ExecutionTime);
                    }
                }
                EndExecution(context);

                return context.HardwareExecutionInformation;
            }
        }


        private OpenClResultMetadata GetResultMetadata(SimpleMemoryAccessor buffer)
        {
            var bufferSpan = buffer.Get(_configuration.HeaderCellCount).Span;

            Logger.LogInformation("_configuration.HeaderCellCount: {0}", _configuration.HeaderCellCount);
            Logger.LogInformation("bufferSpan size: {0}b", bufferSpan.Length);
            var header = bufferSpan.Slice(0, _configuration.HeaderCellCount * MemoryCellSizeBytes);
            var result = new OpenClResultMetadata(header, _configuration.DeviceIsBigEndian);

            bool canLogInfo = Logger.IsEnabled(LogLevel.Information);
            bool canLogDebug = Logger.IsEnabled(LogLevel.Debug);
            if (canLogInfo || canLogDebug)
            {
                bufferSpan = bufferSpan.Slice(_configuration.HeaderCellCount * MemoryCellSizeBytes);

                if (canLogDebug)
                {
                    int logAmount = bufferSpan.Length / MemoryCellSizeBytes;
                    for (int i = 0; i < logAmount; i++)
                    {
                        var value = MemoryMarshal.Read<int>(bufferSpan.Slice(i * MemoryCellSizeBytes));
                        Logger.LogDebug("HOST: buffer[{0}] = 0x{1:X8}", i, value);
                    }
                }

                if (canLogInfo) Logger.LogInformation("Execution time: {0}ms", result.ExecutionTime);
            }

            return result;
        }
    }
}
