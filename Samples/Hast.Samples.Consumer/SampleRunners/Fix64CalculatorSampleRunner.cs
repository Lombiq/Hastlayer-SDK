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
    internal static class Fix64CalculatorSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<Fix64Calculator>();

            configuration.TransformerConfiguration().AddMemberInvocationInstanceCountConfiguration(
                new MemberInvocationInstanceCountConfigurationForMethod<Fix64Calculator>(f => f.ParallelizedCalculateIntegerSumUpToNumber(null), 0)
                {
                    MaxDegreeOfParallelism = Fix64Calculator.MaxDegreeOfParallelism
                });
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            var fixed64Showcase = await hastlayer.GenerateProxy(hardwareRepresentation, new Fix64Calculator());

            var sum = fixed64Showcase.CalculateIntegerSumUpToNumber(10000000);

            // This takes about 264ms on an i7 processor with 4 physical (8 logical) cores and 1300ms on an FPGA (with 
            // a MaxDegreeOfParallelism of 10 while the device is about 60% utilized). With a higher degree of 
            // parallelism it won't fit on the Nexys 4 DDR board's FPGA.
            var sums = fixed64Showcase.ParallelizedCalculateIntegerSumUpToNumber(10000000);
        }
    }
}
