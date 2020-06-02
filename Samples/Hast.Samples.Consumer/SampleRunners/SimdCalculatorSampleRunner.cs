using System;
using System.Linq;
using System.Threading.Tasks;
using Hast.Layer;
using Hast.Samples.SampleAssembly;

namespace Hast.Samples.Consumer.SampleRunners
{
    internal static class SimdCalculatorSampleRunner
    {
        public static void Configure(HardwareGenerationConfiguration configuration)
        {
            configuration.AddHardwareEntryPointType<SimdCalculator>();
        }

        public static async Task Run(IHastlayer hastlayer, IHardwareRepresentation hardwareRepresentation, IProxyGenerationConfiguration configuration)
        {
            // Starting with 1 not to have a divide by zero.
            var vector = Enumerable.Range(1, SimdCalculator.MaxDegreeOfParallelism * 4).ToArray();

            var simdCalculator = await hastlayer.GenerateProxy(hardwareRepresentation, new SimdCalculator(), configuration ?? ProxyGenerationConfiguration.Default);

            var sumVector = ThrowIfNotCorrect(simdCalculator, calculator => calculator.AddVectors(vector, vector));
            var differenceVector = ThrowIfNotCorrect(simdCalculator, calculator => calculator.SubtractVectors(vector, vector));
            var productVector = ThrowIfNotCorrect(simdCalculator, calculator => calculator.MultiplyVectors(vector, vector));
            var quotientVector = ThrowIfNotCorrect(simdCalculator, calculator => calculator.DivideVectors(vector, vector));
        }


        // Checking if the hardware result is correct by running it against the software implementation. Note that this
        // can be also done by Hastlayer automatically on the SimpleMemory level by setting
        // ProxyGenerationConfiguration.VerifyHardwareResults to true when calling GenerateProxy().
        private static int[] ThrowIfNotCorrect(SimdCalculator proxied, Func<SimdCalculator, int[]> operation)
        {
            var hardwareResult = operation(proxied);
            var softwareResult = operation(new SimdCalculator());

            if (hardwareResult.Length != softwareResult.Length)
            {
                throw new InvalidOperationException("The two results' length is not the same.");
            }

            for (int i = 0; i < hardwareResult.Length; i++)
            {
                try
                {
                    if (hardwareResult[i] != softwareResult[i])
                    {
                        // Uncomment this to list errors in a file.
                        //System.IO.File.AppendAllText(
                        //    "Errors.txt",
                        //    i.ToString() + ": " + hardwareResult[i].ToString() + " vs " + softwareResult[i].ToString() + Environment.NewLine);

                        throw new InvalidOperationException(
                            "The hardware result and the software result is not the same on index " + i.ToString() + ": " +
                            hardwareResult[i].ToString() + " vs " + softwareResult[i].ToString() + ".");
                    }
                }
                catch (Exception)
                {
                }
            }

            return hardwareResult;
        }
    }
}
