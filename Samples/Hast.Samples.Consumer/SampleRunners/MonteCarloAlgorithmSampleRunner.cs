using System.Threading.Tasks;
using Hast.Layer;
using Hast.Samples.SampleAssembly;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal static class MonteCarloAlgorithmSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.HardwareEntryPointMemberNamePrefixes.Add("Hast.Samples.SampleAssembly.MonteCarloAlgorithm");
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            var monteCarloAlgorithm = await hastlayer
                .GenerateProxy(hardwareRepresentation, new MonteCarloAlgorithm());
            var monteCarloResult = monteCarloAlgorithm.CalculateTorusSectionValues(5000000);
        }
    }
}
