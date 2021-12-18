using Hast.Layer;
using Hast.Samples.SampleAssembly;
using Hast.Transformer.Abstractions.Configuration;
using System.Threading.Tasks;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal class RecursiveAlgorithmsSampleRunner : ISampleRunner
    {
        public void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<RecursiveAlgorithms>();

            configuration.TransformerConfiguration().AddMemberInvocationInstanceCountConfiguration(
                new MemberInvocationInstanceCountConfigurationForMethod<RecursiveAlgorithms>("Recursively")
                {
                    // If we give these algorithms inputs causing a larger recursion depth then that will cause runtime
                    // problems.
                    MaxRecursionDepth = 20,
                });
        }

        public async Task RunAsync(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            var recursiveAlgorithms = await hastlayer.GenerateProxyAsync(hardwareRepresentation, new RecursiveAlgorithms(), configuration);

            // 720
            _ = recursiveAlgorithms.CalculateFactorial(6, hastlayer, hardwareRepresentation.HardwareGenerationConfiguration);

            // The Fibonacci sample is deactivated because it gives wrong results on hardware.
            // 233
            //// var fibonacci = recursiveAlgorithms.CalculateFibonacchiSeries(
            ////     13,
            ////     hastlayer,
            ////     hardwareRepresentation.HardwareGenerationConfiguration);
        }
    }
}
