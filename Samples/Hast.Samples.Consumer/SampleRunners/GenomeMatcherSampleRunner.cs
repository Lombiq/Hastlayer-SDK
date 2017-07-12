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

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation)
        {
            var genomeMatcher = await hastlayer.GenerateProxy(hardwareRepresentation, new GenomeMatcher());

            // Sample from IBM.
            var inputOne = "GCCCTAGCG";
            var inputTwo = "GCGCAATG";

            var result = genomeMatcher.CalculateLongestCommonSubsequence(inputOne, inputTwo);

            // Sample from Wikipedia.
            inputOne = "ACACACTA";
            inputTwo = "AGCACACA";

            result = genomeMatcher.CalculateLongestCommonSubsequence(inputOne, inputTwo);

            inputOne = "lombiqtech";
            inputTwo = "coulombtech";

            result = genomeMatcher.CalculateLongestCommonSubsequence(inputOne, inputTwo);
        }
    }
}
