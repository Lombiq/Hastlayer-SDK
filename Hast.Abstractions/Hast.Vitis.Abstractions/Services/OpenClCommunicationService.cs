﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Hast.Communication.Models;
using Hast.Communication.Services;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using Hast.Vitis.Abstractions.Extensions;
using Hast.Vitis.Abstractions.Models;
using Microsoft.Extensions.Logging;
using static Hast.Transformer.Abstractions.SimpleMemory.SimpleMemory;

namespace Hast.Vitis.Abstractions.Services
{
    public abstract class OpenClCommunicationService : CommunicationServiceBase
    {
        public const string KernelName = "hastip";


        private readonly IDevicePoolPopulator _devicePoolPopulator;
        private readonly IDevicePoolManager _devicePoolManager;

        protected readonly IBinaryOpenCl _binaryOpenCl;
        protected readonly ILogger _logger;


        protected OpenClCommunicationService(
            IDevicePoolPopulator devicePoolPopulator,
            IDevicePoolManager devicePoolManager,
            IBinaryOpenCl binaryOpenCl,
            ILogger<OpenClCommunicationService> logger) : base(logger)
        {
            _devicePoolPopulator = devicePoolPopulator;
            _devicePoolManager = devicePoolManager;
            _binaryOpenCl = binaryOpenCl;
            _logger = logger;
        }


        public override async Task<IHardwareExecutionInformation> Execute(
            SimpleMemory simpleMemory,
            int memberId,
            IHardwareExecutionContext executionContext)
        {
            var configuration = executionContext
                .HardwareRepresentation
                .HardwareGenerationConfiguration
                .GetOrAddOpenClConfiguration();
            _binaryOpenCl.PrepareDevices(configuration);

            if (!File.Exists(configuration.BinaryFilePath))
            {
                throw new FileNotFoundException("The OpenCL binary (xclbin) is required to start the kernel. The " +
                    $"host can't launch without it. Please make sure the file at '{configuration.BinaryFilePath}' " +
                    "exists and is accessible.");
            }

            var kernelBinary = File.ReadAllBytes(configuration.BinaryFilePath);
            _binaryOpenCl.CreateBinaryKernel(kernelBinary, KernelName);

            _devicePoolPopulator.PopulateDevicePoolIfNew(() =>
            {
                var devices = new List<IDevice>(_binaryOpenCl.DeviceCount);
                for (var i = 0; i < _binaryOpenCl.DeviceCount; i++)
                {
                    devices.Add(new Device
                    {
                        Identifier = $"{ChannelName}:{configuration.VendorName ?? "any"}:{i}", Metadata = i
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
                    var hostMemory = memoryAccessor.Get(configuration.HeaderCellCount);
                    hostMemory.Span.SetIntegers(0, configuration.HeaderCellCount, memberId);

                    using (var hostMemoryHandle = hostMemory.Pin())
                    {
                        // Send data and execute.
                        var fpgaBuffer = _binaryOpenCl.SetKernelArgumentWithNewBuffer(
                            KernelName, 0, hostMemoryHandle, hostMemory.Length, GetBuffer(hostMemory, hostMemoryHandle));
                        Logger.LogInformation("KERNEL #{0} ARGUMENT SET", 0);
                        _binaryOpenCl.LaunchKernel(deviceIndex, KernelName, new[] {fpgaBuffer});
                        await _binaryOpenCl.AwaitDevice(deviceIndex);
                        var resultMetadata = GetResultMetadata(memoryAccessor, configuration);

                        // Read out metadata.
                        SetHardwareExecutionTime(context, executionContext, resultMetadata.ExecutionTime);
                    }
                }
                EndExecution(context);

                return context.HardwareExecutionInformation;
            }
        }


        protected virtual IntPtr GetBuffer(Memory<byte> data, MemoryHandle hostMemoryHandle) => IntPtr.Zero;


        private OpenClResultMetadata GetResultMetadata(SimpleMemoryAccessor buffer, IOpenClConfiguration configuration)
        {
            var bufferSpan = buffer.Get(configuration.HeaderCellCount).Span;

            Logger.LogInformation("_configuration.HeaderCellCount: {0}", configuration.HeaderCellCount);
            Logger.LogInformation("bufferSpan size: {0}b", bufferSpan.Length);
            var header = bufferSpan.Slice(0, configuration.HeaderCellCount * MemoryCellSizeBytes);
            var result = new OpenClResultMetadata(header, configuration.DeviceIsBigEndian);

            bool canLogInfo = Logger.IsEnabled(LogLevel.Information);
            bool canLogDebug = Logger.IsEnabled(LogLevel.Debug);
            if (canLogInfo || canLogDebug)
            {
                bufferSpan = bufferSpan.Slice(configuration.HeaderCellCount * MemoryCellSizeBytes);

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
