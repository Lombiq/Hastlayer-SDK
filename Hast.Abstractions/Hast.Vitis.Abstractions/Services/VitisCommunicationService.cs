using Hast.Communication.Models;
using System;
using System.Buffers;
using Hast.Communication.Services;
using Hast.Vitis.Abstractions.Interop;
using Hast.Vitis.Abstractions.Interop.Enums.OpenCl;
using Hast.Xilinx.Abstractions;
using Microsoft.Extensions.Logging;

namespace Hast.Vitis.Abstractions.Services
{
    public class VitisCommunicationService : OpenClCommunicationService
    {
        public override string ChannelName { get; } = Hast.Xilinx.Abstractions.Constants.VitisCommunicationChannelName;


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
            var isHbm = data.Length <= 256_000_000 &&
                (executionContext.HardwareRepresentation.DeviceManifest as XilinxDeviceManifest)?.SupportsHbm != false;
            _logger.LogInformation($"Using HBM: {isHbm}.");

            if (!isHbm) return IntPtr.Zero;

            const MemoryFlag flags = BinaryOpenCl.DefaultMemoryFlags | MemoryFlag.ExtensionXilinxPointer;
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
