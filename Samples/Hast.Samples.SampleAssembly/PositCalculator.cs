using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// A sample showing how custom-sized floating point numbers of type Posit (<see href="https://posithub.org" />)
    /// can be used with Hastlayer.
    /// </summary>
    /// <remarks>
    /// <para>This sample is added here for future use. At the time statically-sized Posits like Posit32 are better usable,
    /// <see cref="Posit32Calculator"/>.</para>
    /// </remarks>
    public class PositCalculator
    {
        public const int CalculateLargeIntegerSumInputInt32Index = 0;
        public const int CalculateLargeIntegerSumOutputInt32Index = 0;

        public virtual void CalculateIntegerSumUpToNumber(SimpleMemory memory)
        {
            var number = memory.ReadUInt32(CalculateLargeIntegerSumInputInt32Index);

            var environment = EnvironmentFactory();

            var a = new Posit(environment, 1);
            var b = a;

            for (var i = 1; i < number; i++)
            {
                a += b;
            }

            var result = (int)a;
            memory.WriteInt32(CalculateLargeIntegerSumOutputInt32Index, result);
        }

        public static PositEnvironment EnvironmentFactory() => new(32, 3);
    }
}
