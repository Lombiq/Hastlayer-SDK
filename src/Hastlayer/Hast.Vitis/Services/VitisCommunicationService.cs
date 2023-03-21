using Hast.Communication.Models;
using Hast.Communication.Services;
using Hast.Vitis.Extensions;
using Hast.Vitis.Interop;
using Hast.Vitis.Interop.Enums;
using Hast.Xilinx;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.IO;
using static Hast.Vitis.Constants.Extensions;

namespace Hast.Vitis.Services;

public class VitisCommunicationService : OpenClCommunicationService
{
    private const int HbmSizeBytes = (int)Constants.Limits.HbmSizeBytes;
    public override string ChannelName { get; } = Xilinx.Constants.VitisCommunicationChannelName;

    public VitisCommunicationService(
        IDevicePoolPopulator devicePoolPopulator,
        IDevicePoolManager devicePoolManager,
        IBinaryOpenCl binaryOpenCl,
        ILogger<VitisCommunicationService> logger)
        : base(devicePoolPopulator, devicePoolManager, binaryOpenCl, logger) { }

    protected override IntPtr GetBuffer(
        Memory<byte> data,
        MemoryHandle hostMemoryHandle,
        IHardwareExecutionContext executionContext)
    {
        var configuration = executionContext
            .HardwareRepresentation
            .HardwareGenerationConfiguration
            .GetOrAddOpenClConfiguration();

        var vitisDeviceManifest = (VitisDeviceManifest)executionContext.HardwareRepresentation.DeviceManifest;
        var isHbm = configuration.UseHbm && data.Length <= HbmSizeBytes && vitisDeviceManifest.SupportsHbm;

        var implementation = executionContext.HardwareRepresentation.HardwareImplementation;
        if (isHbm && File.Exists(implementation.BinaryPath + NoHbmFlagExtension))
        {
            isHbm = false;
            _logger.LogInformation("HBM was explicitly disabled.");
        }
        else
        {
            _logger.LogInformation("Using HBM: {IsHbm}.", isHbm);
        }

        if (!isHbm) return IntPtr.Zero;

        const MemoryFlags flags = BinaryOpenCl.DefaultMemoryFlags | MemoryFlags.ExtensionXilinxPointer;
        XilinxMemoryExtension bank;
        IntPtr hostPointer;
        unsafe
        {
            bank = XilinxMemoryExtension.Create((IntPtr)hostMemoryHandle.Pointer, 0);
            void* pointer = &bank;
            hostPointer = (IntPtr)pointer;
        }

        return _binaryOpenCl.CreateBuffer(hostPointer, data.Length, flags);
    }
}
