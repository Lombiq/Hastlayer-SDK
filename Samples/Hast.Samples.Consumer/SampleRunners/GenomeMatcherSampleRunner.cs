﻿using System.Threading.Tasks;
using Hast.Layer;
using Hast.Samples.SampleAssembly;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class GenomeMatcherSampleRunner : ISampleRunner
    {
        public void Configure(HardwareGenerationConfiguration configuration) => configuration.AddHardwareEntryPointType<GenomeMatcher>();

        public async Task RunAsync(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            var genomeMatcher = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new GenomeMatcher(), configuration);

            // Sample from IBM.
            var inputOne = "GCCCTAGCG";
            var inputTwo = "GCGCAATG";

            var result = genomeMatcher.CalculateLongestCommonSubsequence(inputOne, inputTwo, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);

            // Sample from Wikipedia.
            inputOne = "ACACACTA";
            inputTwo = "AGCACACA";

            result = genomeMatcher.CalculateLongestCommonSubsequence(inputOne, inputTwo, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);

            inputOne = "lombiqtech";
            inputTwo = "coulombtech";

            result = genomeMatcher.CalculateLongestCommonSubsequence(inputOne, inputTwo, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);
        }
    }
}
