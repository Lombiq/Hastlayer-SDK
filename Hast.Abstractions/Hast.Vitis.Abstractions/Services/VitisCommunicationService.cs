using System;
using System.Buffers;
using Hast.Common.Interfaces;
using Hast.Communication.Services;
using Hast.Vitis.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using Hast.Transformer.Abstractions.SimpleMemory;
using Hast.Vitis.Abstractions.Interop;
using Hast.Vitis.Abstractions.Interop.Enums.OpenCl;

namespace Hast.Vitis.Abstractions.Services
{
    [IDependencyInitializer(nameof(InitializeService))]
    public class VitisCommunicationService : OpenClCommunicationService
    {
        public const string ConfigFileName = nameof(VitisCommunicationService) + ".json";

        public override string ChannelName { get; } = Xilinx.Abstractions.Constants.VitisCommunicationChannelName;


        public VitisCommunicationService(
            IDevicePoolPopulator devicePoolPopulator,
            IDevicePoolManager devicePoolManager,
            IBinaryOpenCl binaryOpenCl,
            IOpenClConfiguration configuration,
            ILogger<VitisCommunicationService> logger)
            : base(devicePoolPopulator, devicePoolManager, binaryOpenCl, configuration, logger) { }



        public static void InitializeService(IServiceCollection services)
        {
            var config = new OpenClConfiguration();
            if (File.Exists(ConfigFileName))
            {
                var json = File.ReadAllText(ConfigFileName);
                config = JsonConvert.DeserializeObject<OpenClConfiguration>(json);
            }
            else
            {
                var json = JsonConvert.SerializeObject(config);
                File.WriteAllText(ConfigFileName, json);
            }

            SimpleMemory.Alignment = config.MemoryAlignment;

            services.AddSingleton<IOpenClConfiguration>(config);
        }


        protected override IntPtr GetBuffer(Memory<byte> data, MemoryHandle hostMemoryHandle)
        {
            _logger.LogInformation($"Using HBM: {data.Length <= 256_000_000}");
            if (data.Length > 256_000_000) return IntPtr.Zero;

            var flags = BinaryOpenCl.DefaultMemoryFlags | MemoryFlag.ExtensionXilinxPointer;
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
