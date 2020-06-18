using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Lombiq.Arithmetics;

namespace Hast.Samples.Consumer.SampleRunners
{
    /// <summary>
    /// See <see cref="Posit32CalculatorSampleRunner"/> for a more usable example.
    /// </summary>
    internal class PositCalculatorSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<PositCalculator>();

            configuration.TransformerConfiguration().AddLengthForMultipleArrays(
                PositCalculator.EnvironmentFactory().EmptyBitMask.SegmentCount,
                PositCalculatorExtensions.ManuallySizedArrays);
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            var positCalculator = await hastlayer.GenerateProxy(hardwareRepresentation, new PositCalculator(), configuration);

            var memoryConfig = (hastlayer as Hastlayer).CreateMemoryConfiguration(hardwareRepresentation);
            var result = positCalculator.CalculateIntegerSumUpToNumber(100000, memoryConfig);
        }
    }
}
