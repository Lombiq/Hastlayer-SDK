﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Samples.SampleAssembly;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class LoopbackSampleRunner : ISampleRunner
    {
        public void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<Loopback>();
        }

        public async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation,
            IProxyGenerationConfiguration configuration)
        {
            var loopback = await hastlayer.GenerateProxy(hardwareRepresentation, new Loopback(), configuration);

            var output1 = loopback.Run(123, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            var output2 = loopback.Run(1234, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            var output3 = loopback.Run(-9, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            var output4 = loopback.Run(0, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            var output5 = loopback.Run(-19, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
            var output6 = loopback.Run(1, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        }
    }
}
