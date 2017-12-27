using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Samples.SampleAssembly;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class Posit32CalculatorSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<PositCalculator>();
            configuration.TransformerConfiguration().AddLengthForMultipleArrays(
    PositCalculator.EnvironmentFactory().EmptyBitMask.SegmentCount,
    PositCalculatorExtensions.ManuallySizedArrays);
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            var positCalculator = await hastlayer.GenerateProxy(hardwareRepresentation, new PositCalculator());

            var result = positCalculator.CountUpToNumber(100000);
        }
    }
}
