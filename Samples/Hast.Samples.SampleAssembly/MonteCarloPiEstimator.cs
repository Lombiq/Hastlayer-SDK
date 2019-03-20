using Hast.Algorithms;
using Hast.Transformer.Abstractions.SimpleMemory;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// Algorithm to calculate Pi with a random <see href="https://en.wikipedia.org/wiki/Monte_Carlo_method"> Monte
    /// Carlo method</see> in a parallelized manner. For an overview  of the idea see <see
    /// href="https://www.coursera.org/lecture/parprog1/monte-carlo-method-to-estimate-pi-Zgm76">this video</see>;
    /// <see href="http://www.software-architects.com/devblog/2014/09/22/C-Parallel-and-Async-Programming"/>this
    /// blogpost's implementation</see> was used as an inspiration too. Also see <see
    /// cref="MonteCarloAlgorithmSampleRunner"/> on what to configure to make this work.
    /// </summary>
    public class MonteCarloPiEstimator
    {
        public const int MaxDegreeOfParallelism = 13;
        public const int EstimatePi_IteractionsCountUInt32Index = 0;
        public const int EstimatePi_RandomSeedUInt32Index = 1;
        public const int EstimatePi_PiEstimateFix64StartIndex = 0;


        public virtual void EstimatePi(SimpleMemory memory)
        {
            var iterationsCount = memory.ReadUInt32(EstimatePi_IteractionsCountUInt32Index);
            var randomSeed = memory.ReadUInt32(EstimatePi_RandomSeedUInt32Index);

            var iterationsPerTask = iterationsCount / MaxDegreeOfParallelism;
            var range = 0xFFFFFFu;
            var tasks = new Task<uint>[MaxDegreeOfParallelism];

            for (uint i = 0; i < MaxDegreeOfParallelism; i++)
            {
                tasks[i] = Task.Factory.StartNew(
                    indexObject =>
                    {
                        var index = (uint)indexObject;
                        var random = new PrngMWC64X(randomSeed + index);

                        uint inCircleCount = 0;

                        for (var j = 0; j < iterationsPerTask; j++)
                        {
                            // While we can use floating or fixed point numbers to represent fractions as well it's 
                            // more efficient to compute with scaled integers.
                            ulong a = random.NextUInt32() % range;
                            ulong b = random.NextUInt32() % range;

                            if (a * a + b * b <= ((ulong)range * range))
                            {
                                inCircleCount++;
                            }
                        }

                        return inCircleCount;
                    },
                    i);
            }

            Task.WhenAll(tasks).Wait();

            uint inCircleCountSum = 0;
            for (int i = 0; i < MaxDegreeOfParallelism; i++)
            {
                inCircleCountSum += tasks[i].Result;
            }


            memory.WriteUInt32(EstimatePi_PiEstimateFix64StartIndex, inCircleCountSum);
        }
    }


    public static class MonteCarloPiEstimatorExtensions
    {
        private static Random _random = new Random();


        public static double EstimatePi(this MonteCarloPiEstimator piEstimator, uint iterationsCount)
        {
            if (iterationsCount % MonteCarloPiEstimator.MaxDegreeOfParallelism != 0)
            {
                throw new Exception($"The number of iterations must be divisible by {MonteCarloPiEstimator.MaxDegreeOfParallelism}.");
            }

            var memory = new SimpleMemory(2);
            memory.WriteUInt32(MonteCarloPiEstimator.EstimatePi_IteractionsCountUInt32Index, iterationsCount);
            memory.WriteUInt32(MonteCarloPiEstimator.EstimatePi_RandomSeedUInt32Index, (uint)_random.Next(0, int.MaxValue));

            piEstimator.EstimatePi(memory);

            // This single calculation takes up too much space on the FPGA, since it needs fix point arithmetic, but
            // it doesn't take much time. So doing it on the host instead.
            return (double)memory.ReadInt32(MonteCarloPiEstimator.EstimatePi_PiEstimateFix64StartIndex) / iterationsCount * 4;
        }
    }
}
