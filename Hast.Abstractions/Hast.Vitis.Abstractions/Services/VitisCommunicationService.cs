using Hast.Communication.Models;
using Hast.Communication.Services;
using Hast.Vitis.Abstractions.Extensions;
using Hast.Vitis.Abstractions.Interop;
using Hast.Vitis.Abstractions.Interop.Enums.OpenCl;
using Hast.Xilinx.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.IO;
using static Hast.Vitis.Abstractions.Constants.Extensions;

namespace Hast.Vitis.Abstractions.Services
{
    public class VitisCommunicationService : OpenClCommunicationService
    {
        private const int HbmSizeBytes = (int)Constants.Limits.HbmSizeBytes;
        public override string ChannelName { get; } = Xilinx.Abstractions.Constants.VitisCommunicationChannelName;

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

            var isHbm = configuration.UseHbm &&
                data.Length <= HbmSizeBytes &&
                (executionContext.HardwareRepresentation.DeviceManifest as XilinxDeviceManifest)?.SupportsHbm != false;

            var implementation = executionContext.HardwareRepresentation.HardwareImplementation;
            if (isHbm && File.Exists(implementation.BinaryPath + NoHbmFlagExtension))
            {
                isHbm = false;
                _logger.LogInformation("HBM was explicitly disabled.");
            }
            else
            {
                _logger.LogInformation($"Using HBM: {isHbm}.");
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
}
