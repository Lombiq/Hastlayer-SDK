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
                    // If we give these algorithms inputs causing a larger recursion depth then that will
                    // cause runtime problems.
                    MaxRecursionDepth = 20
                });
        }

        public async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            var recursiveAlgorithms = await hastlayer.GenerateProxy(hardwareRepresentation, new RecursiveAlgorithms(), configuration);

            var memoryConfig = (hastlayer as Hastlayer).CreateMemoryConfiguration(hardwareRepresentation);
            var factorial = recursiveAlgorithms.CalculateFactorial(6, memoryConfig); // 720
            var fibonacci = recursiveAlgorithms.CalculateFibonacchiSeries(13, memoryConfig); // 233
        }
    }
}
