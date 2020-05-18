using Hast.Common.Interfaces;
using Hast.Communication.Services;
using Hast.Vitis.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Vitis.Abstractions.Services
{
    [IDependencyInitializer(nameof(InitializeService))]
    public class VitisCommunicationService : OpenClCommunicationService
    {
        public const string ConfigFileName = nameof(VitisCommunicationService) + ".json";

        public override string ChannelName { get; } = Xilinx.Abstractions.Constants.SDAccelCommunicationChannelName;

        public VitisCommunicationService(
            IDevicePoolPopulator devicePoolPopulator,
            IDevicePoolManager devicePoolManager,
            IBinaryOpenCl binaryOpenCl,
            IOpenClConfiguration configuration,
            ILogger logger)
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
    }
}
