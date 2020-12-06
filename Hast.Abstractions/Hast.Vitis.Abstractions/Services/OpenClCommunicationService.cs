using Hast.Communication.Models;
using Hast.Communication.Services;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using Hast.Vitis.Abstractions.Extensions;
using Hast.Vitis.Abstractions.Interop.Enums;
using Hast.Vitis.Abstractions.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Hast.Transformer.Abstractions.SimpleMemory.SimpleMemory;
using static Hast.Vitis.Abstractions.Constants.Extensions;

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

            var implementation = executionContext.HardwareRepresentation.HardwareImplementation;
            if (!File.Exists(implementation.BinaryPath))
            {
                throw new FileNotFoundException(
                    "The OpenCL binary (xclbin) is required to start the kernel. The host can't launch without it. " +
                    $"Please make sure the file at '{implementation.BinaryPath}' exists and is accessible.");
            }

            uint? clockFrequency = null;
            var infoFilePath = implementation.BinaryPath + InfoFileExtension;
            if (!File.Exists(infoFilePath))
            {
                Logger.LogWarning(
                    "The info file is required to learn the kernel clock frequency and report the execution time " +
                    "accurately. Please copy it to '{0}'! (see `xclbinutil --info --input XCLBIN_FILE_PATH`)",
                    infoFilePath);
            }
            else
            {
                using var stream = new FileStream(infoFilePath, FileMode.Open);
                clockFrequency = XclbinClockInfo.FromStream(stream, Encoding.Default)
                    .FirstOrDefault(info => info.Type == XclbinClockInfoType.Data)?
                    .Frequency;
            }

            var kernelBinary = await File.ReadAllBytesAsync(implementation.BinaryPath);
            _binaryOpenCl.CreateBinaryKernel(kernelBinary, KernelName);

            _devicePoolPopulator.PopulateDevicePoolIfNew(() =>
            {
                var devices = new List<IDevice>(_binaryOpenCl.DeviceCount);
                for (var i = 0; i < _binaryOpenCl.DeviceCount; i++)
                {
                    devices.Add(new Device
                    {
                        Identifier = $"{ChannelName}:{configuration.VendorName ?? "any"}:{i}",
                        Metadata = i,
                    });
                    _binaryOpenCl.CreateCommandQueue(i);
                }

                return Task.FromResult<IEnumerable<IDevice>>(devices);
            });

            using var device = await _devicePoolManager.ReserveDevice();
            var context = BeginExecution();
            {
                int deviceIndex = device.Metadata;
                var memoryAccessor = new SimpleMemoryAccessor(simpleMemory);

                // Prepare host buffer.
                var hostMemory = memoryAccessor.Get(configuration.HeaderCellCount);
                hostMemory.Span.SetIntegers(0, configuration.HeaderCellCount, memberId);

                Logger.LogInformation("Input buffer size: {0}b", hostMemory.Length);
                var headerSize = configuration.HeaderCellCount * MemoryCellSizeBytes;
                if (hostMemory.Length <= headerSize)
                {
                    throw new ArgumentOutOfRangeException(
                        $"The result size is only {hostMemory.Length}b but it must be more than the header size of " +
                        $"{headerSize}b.");
                }

                using var hostMemoryHandle = hostMemory.Pin();
                // Send data and execute.
                var fpgaBuffer = _binaryOpenCl.SetKernelArgumentWithNewBuffer(
                    KernelName,
                    index: 0,
                    hostMemoryHandle,
                    hostMemory.Length,
                    GetBuffer(hostMemory, hostMemoryHandle, executionContext));
                Logger.LogInformation("KERNEL ARGUMENT #{0} SET", 0);
                Logger.LogInformation("LAUNCHING KERNEL...");
                _binaryOpenCl.LaunchKernel(deviceIndex, KernelName, new[] { fpgaBuffer });
                Logger.LogInformation("KERNEL LAUNCHED, AWAITING RESULTS");
                await _binaryOpenCl.AwaitDevice(deviceIndex);
                var resultMetadata = GetResultMetadata(hostMemory.Span, configuration);

                // Read out metadata.
                SetHardwareExecutionTime(
                    context,
                    executionContext,
                    resultMetadata.ExecutionTime,
                    clockFrequency);
            }

            EndExecution(context);

            return context.HardwareExecutionInformation;
        }

        protected virtual IntPtr GetBuffer(
            Memory<byte> data,
            MemoryHandle hostMemoryHandle,
            IHardwareExecutionContext executionContext) =>
            IntPtr.Zero;

        private OpenClResultMetadata GetResultMetadata(Span<byte> bufferSpan, IOpenClConfiguration configuration)
        {
            Logger.LogInformation("_configuration.HeaderCellCount: {0}", configuration.HeaderCellCount);
            Logger.LogInformation("HeaderCellCount reported by output: {0}", MemoryMarshal.Read<uint>(bufferSpan));
            Logger.LogInformation("Output buffer size: {0}b", bufferSpan.Length);

            var headerSize = configuration.HeaderCellCount * MemoryCellSizeBytes;
            if (bufferSpan.Length <= headerSize)
            {
                throw new ArgumentOutOfRangeException(
                    $"The result size is only {bufferSpan.Length}b but it must be more than the header size of {headerSize}b.");
            }

            var header = bufferSpan.Slice(0, headerSize);
            var result = new OpenClResultMetadata(header, configuration.DeviceIsBigEndian);

            bool canLogInfo = Logger.IsEnabled(LogLevel.Information);
            bool canLogDebug = Logger.IsEnabled(LogLevel.Debug);
            if (canLogInfo || canLogDebug)
            {
                bufferSpan = bufferSpan[headerSize..];

                if (canLogDebug)
                {
                    int logAmount = bufferSpan.Length / MemoryCellSizeBytes;
                    for (int i = 0; i < logAmount; i++)
                    {
                        var value = MemoryMarshal.Read<int>(bufferSpan[(i * MemoryCellSizeBytes)..]);
                        Logger.LogDebug("HOST: buffer[{0}] = 0x{1:X8}", i, value);
                    }
                }

                if (canLogInfo) Logger.LogInformation("Execution time: {0} cycles", result.ExecutionTime);
            }

            return result;
        }
    }
}
