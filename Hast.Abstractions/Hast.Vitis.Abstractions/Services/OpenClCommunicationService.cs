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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Hast.Transformer.Abstractions.SimpleMemory.SimpleMemory;
using static Hast.Vitis.Abstractions.Constants.Extensions;

namespace Hast.Vitis.Abstractions.Services;

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
        ILogger<OpenClCommunicationService> logger)
        : base(logger)
    {
        _devicePoolPopulator = devicePoolPopulator;
        _devicePoolManager = devicePoolManager;
        _binaryOpenCl = binaryOpenCl;
        _logger = logger;
    }

    public override async Task<IHardwareExecutionInformation> ExecuteAsync(
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

        uint? clockFrequency = await GetClockFrequencyAsync(implementation);

        var kernelBinary = await File.ReadAllBytesAsync(implementation.BinaryPath);
        _binaryOpenCl.CreateBinaryKernel(kernelBinary, KernelName);

        _devicePoolPopulator.PopulateDevicePoolIfNew(() =>
        {
            var devices = new List<IDevice>(_binaryOpenCl.DeviceCount);
            for (var i = 0; i < _binaryOpenCl.DeviceCount; i++)
            {
                devices.Add(new Device
                {
                    Identifier = FormattableString.Invariant($"{ChannelName}:{configuration.VendorName ?? "any"}:{i}"),
                    Metadata = i,
                });
                _binaryOpenCl.CreateCommandQueue(i);
            }

            return Task.FromResult<IEnumerable<IDevice>>(devices);
        });

        using var device = await _devicePoolManager.ReserveDeviceAsync();
        var context = BeginExecution();

        int deviceIndex = device.Metadata;
        var memoryAccessor = new SimpleMemoryAccessor(simpleMemory);

        // Prepare host buffer.
        var hostMemory = memoryAccessor.Get(configuration.HeaderCellCount);
        hostMemory.Span.SetIntegers(0, configuration.HeaderCellCount, memberId);
        var hostMemoryLength = hostMemory.Length;
        var timeHostBufferPrepared = context.Stopwatch.ElapsedMilliseconds;

        Logger.LogInformation("Input buffer size: {HostMemoryLength} b", hostMemoryLength);
        var timeLogOverheadTest = context.Stopwatch.ElapsedMilliseconds;

        var headerSize = configuration.HeaderCellCount * MemoryCellSizeBytes;
        if (hostMemory.Length <= headerSize)
        {
            throw new InvalidOperationException(
                FormattableString.Invariant(
                    $"The result size is only {hostMemory.Length}b but it must be more than the header size of {headerSize}b."));
        }

        var timeHostMemoryVerified = context.Stopwatch.ElapsedMilliseconds;

        using var hostMemoryHandle = hostMemory.Pin();
        var timeHostMemoryPinned = context.Stopwatch.ElapsedMilliseconds;

        // Send data and execute.
        var buffer = GetBuffer(hostMemory, hostMemoryHandle, executionContext);
        var timeXilinxBufferInited = context.Stopwatch.ElapsedMilliseconds;
        var fpgaBuffer = _binaryOpenCl.SetKernelArgumentWithNewBuffer(
            KernelName,
            index: 0,
            hostMemoryHandle,
            hostMemory.Length,
            buffer);
        var timeKernelArgumentSet = context.Stopwatch.ElapsedMilliseconds;
        Logger.LogInformation("KERNEL ARGUMENT #{Argument} SET", 0);
        Logger.LogInformation("LAUNCHING KERNEL...");
        _binaryOpenCl.LaunchKernel(deviceIndex, KernelName, new[] { fpgaBuffer });
        Logger.LogInformation("KERNEL LAUNCHED, AWAITING RESULTS");
        var timeKernelLaunched = context.Stopwatch.ElapsedMilliseconds;
        await _binaryOpenCl.AwaitDeviceAsync(deviceIndex);
        var timeResultsAwaited = context.Stopwatch.ElapsedMilliseconds;
        var resultMetadata = GetResultMetadata(hostMemory.Span, configuration);
        var timeMetadataRetrieved = context.Stopwatch.ElapsedMilliseconds;

        // Read out metadata.
        SetHardwareExecutionTime(
            context,
            executionContext,
            resultMetadata.ExecutionTime,
            clockFrequency);
        var timeMetadataProcessed = context.Stopwatch.ElapsedMilliseconds;

        EndExecution(context);
        Logger.LogInformation(
            @"
/--------------------------------------\
| EXECUTION TIME STOPWATCH BREAKDOWN   |
|======================================|
| Host Buffer Prepared      : {TimeHostBufferPrepared,5:####0} ms |
| Log Overhead Test         : {TimeLogOverheadTest,5:####0} ms | *
| Host Memory Verified      : {TimeHostMemoryVerified,5:####0} ms |
| Host Memory Pinned        : {TimeHostMemoryPinned,5:####0} ms |
| Xilinx Buffer Initialized : {TimeXilinxBufferInited,5:####0} ms |
| Kernel Argument Set       : {TimeKernelArgumentSet,5:####0} ms |
| Kernel Launched           : {TimeKernelLaunched,5:####0} ms |
| Results Awaited           : {TimeResultsAwaited,5:####0} ms |
| Metadata Retrieved        : {TimeMetadataRetrieved,5:####0} ms |
| Metadata Processed        : {TimeMetadataProcessed,5:####0} ms |
|--------------------------------------|
| Total                     : {TimeTotal,5:####0} ms |
\--------------------------------------/

(*The time it took to output the first Log.LogInformation call.)

",
            timeHostBufferPrepared,
            timeLogOverheadTest - timeHostBufferPrepared,
            timeHostMemoryVerified - timeLogOverheadTest,
            timeHostMemoryPinned - timeHostMemoryVerified,
            timeXilinxBufferInited - timeHostMemoryPinned,
            timeKernelArgumentSet - timeXilinxBufferInited,
            timeKernelLaunched - timeKernelArgumentSet,
            timeResultsAwaited - timeKernelLaunched,
            timeMetadataRetrieved - timeResultsAwaited,
            timeMetadataProcessed - timeMetadataRetrieved,
            timeMetadataProcessed);

        return context.HardwareExecutionInformation;
    }

    protected virtual IntPtr GetBuffer(
        Memory<byte> data,
        MemoryHandle hostMemoryHandle,
        IHardwareExecutionContext executionContext) =>
        IntPtr.Zero;

    private async Task<uint?> GetClockFrequencyAsync(IHardwareImplementation implementation)
    {
        var infoFilePath = implementation.BinaryPath + InfoFileExtension;

        if (!File.Exists(infoFilePath))
        {
            Logger.LogWarning(
                "The info file is required to learn the kernel clock frequency and report the execution time " +
                "accurately. Please copy it to '{InfoFilePath}'! (see `xclbinutil --info --input XCLBIN_FILE_PATH`)",
                infoFilePath);
            return null;
        }

        await using var stream = new FileStream(infoFilePath, FileMode.Open);
        var clockFrequency = XclbinClockInfo.FromStream(stream, Encoding.Default)
            .FirstOrDefault(info => info.Type == XclbinClockInfoType.Data)?
            .Frequency;

        if (clockFrequency == null)
        {
            _logger.LogWarning("Unknown clock frequency!");
            return null;
        }

        if (!File.Exists(implementation.BinaryPath + SetScaleExtension)) return clockFrequency;

        var setScaleFilePath = (await File.ReadAllTextAsync(implementation.BinaryPath + SetScaleExtension))?.Trim();
        if (setScaleFilePath != null && File.Exists(setScaleFilePath))
        {
            await File.WriteAllTextAsync(
                setScaleFilePath,
                clockFrequency.Value.ToString(CultureInfo.InvariantCulture));

            var frequency = await File.ReadAllTextAsync(setScaleFilePath);
            var frequencyHz = uint.Parse(frequency.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture);
            _logger.LogInformation("Frequency is set to {Frequency:0.##} MHz.", frequencyHz / 1_000_000.0);
            return frequencyHz;
        }

        _logger.LogWarning(
            "The frequency setter system file at the path '{Path}' doesn't exist.",
            setScaleFilePath ?? "<NULL>");
        return clockFrequency;
    }

    private OpenClResultMetadata GetResultMetadata(Span<byte> bufferSpan, IOpenClConfiguration configuration)
    {
        Logger.LogInformation("_configuration.HeaderCellCount: {HeaderCellCount}", configuration.HeaderCellCount);
        Logger.LogInformation("HeaderCellCount reported by output: {HeaderCellCount}", MemoryMarshal.Read<uint>(bufferSpan));
        Logger.LogInformation("Output buffer size: {Length} b", bufferSpan.Length);

        var headerSize = configuration.HeaderCellCount * MemoryCellSizeBytes;
        if (bufferSpan.Length <= headerSize)
        {
            throw new InvalidOperationException(
                FormattableString.Invariant(
                $"The result size is only {bufferSpan.Length}b but it must be more than the header size of {headerSize}b."));
        }

        var header = bufferSpan[..headerSize];
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
                    Logger.LogDebug("HOST: buffer[{Index}] = 0x{Value:X8}", i, value);
                }
            }

            if (canLogInfo) Logger.LogInformation("Execution time: {ExecutionTime} cycles", result.ExecutionTime);
        }

        return result;
    }
}
