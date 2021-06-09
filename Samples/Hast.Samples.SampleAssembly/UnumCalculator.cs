using Hast.Transformer.Abstractions.SimpleMemory;
using Lombiq.Arithmetics;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// A sample on using unum floating point numbers. <see href="http://www.johngustafson.net/unums.html">Some info on unums</see>.
    /// </summary>
    public class UnumCalculator
    {
        public const int CalculateSumOfPowersofTwoInputUInt32Index = 0;
        public const int CalculateSumOfPowersofTwoOutputUInt32Index = 0;

        public virtual void CalculateSumOfPowersofTwo(SimpleMemory memory)
        {
            var number = memory.ReadUInt32(CalculateSumOfPowersofTwoInputUInt32Index);

            var environment = EnvironmentFactory();

            var a = new Unum(environment, 1);
            var b = new Unum(environment, 0);

            for (var i = 1; i <= number; i++)
            {
                b += a;
                a += a;
            }

            var resultArray = b.FractionToUintArray();
            for (var i = 0; i < resultArray.Length; i++)
            {
                memory.WriteUInt32(CalculateSumOfPowersofTwoOutputUInt32Index + i, resultArray[i]);
            }
        }

        // Needed so UnumCalculatorSampleRunner can retrieve BitMask.SegmentCount.
        // On the Nexys 4 DDR only a total of 6b environment will fit and work (9b would fit but wouldn't execute for
        // some reason).
        public static UnumEnvironment EnvironmentFactory() => new(2, 4);
    }
}
