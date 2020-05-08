using Hast.Communication.Models;
using Hast.Communication.Services;
using Hast.Layer;
using Hast.SDAccel.Abstractions.Models;
using Hast.Transformer.Abstractions.SimpleMemory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Hast.Transformer.Abstractions.SimpleMemory.SimpleMemory;

namespace Hast.SDAccel.Abstractions.Services
{
    public abstract class OpenClCommunicationService : CommunicationServiceBase
    {
        public const string KernelName = "hastip";


        private readonly IDevicePoolPopulator _devicePoolPopulator;
        private readonly IDevicePoolManager _devicePoolManager;

        private readonly IBinaryOpenCl _binaryOpenCl;
        private readonly IOpenClConfiguration _configuration;
        private readonly ILogger _logger;


        protected OpenClCommunicationService(
            IDevicePoolPopulator devicePoolPopulator,
            IDevicePoolManager devicePoolManager,
            IBinaryOpenCl binaryOpenCl,
            IOpenClConfiguration configuration,
            ILogger logger)
        {
            _devicePoolPopulator = devicePoolPopulator;
            _devicePoolManager = devicePoolManager;
            _binaryOpenCl = binaryOpenCl;
            _configuration = configuration;
            _logger = logger;
        }


        public override async Task<IHardwareExecutionInformation> Execute(SimpleMemory simpleMemory, int memberId, IHardwareExecutionContext executionContext)
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
                    SetInteger(hostMemory.Span, 0, _configuration.HeaderCellCount);
                    SetInteger(hostMemory.Span, 0, memberId);

                    // Send data and execute.
                    LaunchWithBuffer(deviceIndex, kernelName, hostMemory.Span);
                    await _binaryOpenCl.AwaitDevice(deviceIndex);
                    var resultMetadata = GetResultMetadata(memoryAccessor);

                    // Read out metadata.
                    SetHardwareExecutionTime(context, executionContext, resultMetadata.ExecutionTime);
                    Logger.LogInformation("Incoming data size in bytes: {0}", GetPayloadCellCount(hostMemory.Span));
                }
                EndExecution(context);

                return context.HardwareExecutionInformation;
            }
        }


        private OpenClResultMetadata GetResultMetadata(SimpleMemoryAccessor buffer)
        {
            var bufferSpan = buffer.Get(_configuration.HeaderCellCount).Span;

            _logger.LogInformation("_configuration.HeaderCellCount: {0}", _configuration.HeaderCellCount);
            _logger.LogInformation("bufferSpan size: {0}b", bufferSpan.Length);
            var header = bufferSpan.Slice(0, _configuration.HeaderCellCount * MemoryCellSizeBytes);
            var result = new OpenClResultMetadata(header, _configuration.DeviceIsBigEndian);

            bool canLogInfo = _logger.IsEnabled(LogLevel.Information);
            bool canLogDebug = _logger.IsEnabled(LogLevel.Debug);
            if (canLogInfo || canLogDebug)
            {
                bufferSpan = bufferSpan.Slice(_configuration.HeaderCellCount * MemoryCellSizeBytes);

                if (canLogDebug)
                {
                    int logAmount = bufferSpan.Length / MemoryCellSizeBytes;
                    for (int i = 0; i < logAmount; i++)
                    {
                        var value = MemoryMarshal.Read<int>(bufferSpan.Slice(i * MemoryCellSizeBytes));
                        _logger.LogDebug("HOST: buffer[{0}] = 0x{1:X8}", i, value);
                    }
                }

                if (canLogInfo) _logger.LogInformation("Execution time: {0}ms", result.ExecutionTime);
            }

            return result;
        }

        private int GetPayloadCellCount(Span<byte> buffer) => buffer.Length / MemoryCellSizeBytes - _configuration.HeaderCellCount;

        private void LaunchWithBuffer(int deviceIndex, string kernelName, Span<byte> buffer)
        {
            var fpgaBuffer = _binaryOpenCl.SetKernelArgumentWithNewBuffer(kernelName, 0, buffer);
            _logger.LogInformation("KERNEL #{0} ARGUMENT SET", 0);

            _binaryOpenCl.LaunchKernel(deviceIndex, kernelName, new[] { fpgaBuffer });
        }

        public static void SetInteger(Span<byte> buffer, int index, int value)
        {
            MemoryMarshal.Write(buffer.Slice(index * sizeof(int), sizeof(int)), ref value);
        }
    }
}
