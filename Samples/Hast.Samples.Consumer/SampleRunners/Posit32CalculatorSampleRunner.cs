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
    internal class Posit32CalculatorSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<Posit32Calculator>();
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            var positCalculator = await hastlayer.GenerateProxy(hardwareRepresentation, new Posit32Calculator());

            var result = positCalculator.CountUpToNumber(100000);

            var posit32Array = new uint[100000];

            for (var i = 0; i < 100000; i++)
            {
                if (i % 2 == 0) posit32Array[i] = new Posit32((float)0.25 * 2 * i).PositBits;
                else posit32Array[i] = new Posit32((float)0.25 * -2 * i).PositBits;
            }
            var result2 = positCalculator.AddPositsInArray(posit32Array);
        }
    }
}
