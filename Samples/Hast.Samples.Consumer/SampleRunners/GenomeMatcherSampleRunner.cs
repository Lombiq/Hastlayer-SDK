using System.Threading.Tasks;
using Hast.Layer;
using Hast.Samples.SampleAssembly;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal static class GenomeMatcherSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<GenomeMatcher>();
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            var genomeMatcher = await hastlayer.GenerateProxy(hardwareRepresentation, new GenomeMatcher(), configuration);

            // Sample from IBM.
            var inputOne = "GCCCTAGCG";
            var inputTwo = "GCGCAATG";

            var memoryConfig = (hastlayer as Hastlayer).CreateMemoryConfiguration(hardwareRepresentation);
            var result = genomeMatcher.CalculateLongestCommonSubsequence(inputOne, inputTwo, memoryConfig);

            // Sample from Wikipedia.
            inputOne = "ACACACTA";
            inputTwo = "AGCACACA";

            result = genomeMatcher.CalculateLongestCommonSubsequence(inputOne, inputTwo, memoryConfig);

            inputOne = "lombiqtech";
            inputTwo = "coulombtech";

            result = genomeMatcher.CalculateLongestCommonSubsequence(inputOne, inputTwo, memoryConfig);
        }
    }
}
