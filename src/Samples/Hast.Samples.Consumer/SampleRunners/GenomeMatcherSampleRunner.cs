using Hast.Layer;
using Hast.Samples.SampleAssembly;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners;

internal sealed class GenomeMatcherSampleRunner : ISampleRunner
{
    public void Configure(HardwareGenerationConfiguration configuration) =>
        configuration.AddHardwareEntryPointType<GenomeMatcher>();

    public async Task RunAsync(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
    {
        var genomeMatcher = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new GenomeMatcher(), configuration);

        // Sample from IBM.
        var inputOne = "GCCCTAGCG";
        var inputTwo = "GCGCAATG";
        _ = genomeMatcher.CalculateLongestCommonSubsequence(
            inputOne,
            inputTwo,
            hastlayer,
            hardwareRepresentation.HardwareGenerationConfiguration);

        // Sample from Wikipedia.
        inputOne = "ACACACTA";
        inputTwo = "AGCACACA";

        _ = genomeMatcher.CalculateLongestCommonSubsequence(
            inputOne,
            inputTwo,
            hastlayer,
            hardwareRepresentation.HardwareGenerationConfiguration);

        inputOne = "lombiqtech";
        inputTwo = "coulombtech";

        _ = genomeMatcher.CalculateLongestCommonSubsequence(
            inputOne,
            inputTwo,
            hastlayer,
            hardwareRepresentation.HardwareGenerationConfiguration);
    }
}
