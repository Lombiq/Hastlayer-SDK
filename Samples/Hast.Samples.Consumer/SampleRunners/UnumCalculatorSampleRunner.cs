using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hast.Transformer.Abstractions.Configuration;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class UnumCalculatorSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<UnumCalculator>();

            configuration.TransformerConfiguration().AddLengthForMultipleArrays(
                UnumCalculator.EnvironmentFactory().EmptyBitMask.SegmentCount,
                UnumCalculatorExtensions.ManuallySizedArrays);
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            var unumCalculator = await hastlayer.GenerateProxy(hardwareRepresentation, new UnumCalculator(), configuration ?? ProxyGenerationConfiguration.Default);

            var result = unumCalculator.CalculateSumOfPowersofTwo(9);
        }
    }
}
