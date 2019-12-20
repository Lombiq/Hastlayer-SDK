using Hast.Algorithms.Random;
using Hast.Transformer.Abstractions.SimpleMemory;
using System;
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
        private const int EstimatePi_IteractionsCountUInt32Index = 0;
        private const int EstimatePi_RandomSeedUInt32Index = 1;
        private const int EstimatePi_InCircleCountSumUInt32Index = 0;

        // With 78 about 61% of resources are used on a Nexys 4 DDR, but with 79 101%, so this is the limit of 
        // efficiency. On Catapult, however, 350 will fit (which uses 58% of the logic cells but almost all of the
        // DSPs, so more won't fit).
        public const int MaxDegreeOfParallelism = 78;


        public virtual void EstimatePi(SimpleMemory memory)
        {
            var iterationsCount = memory.ReadUInt32(EstimatePi_IteractionsCountUInt32Index);
            var randomSeed = (ushort)memory.ReadUInt32(EstimatePi_RandomSeedUInt32Index);
            var iterationsPerTask = iterationsCount / MaxDegreeOfParallelism;
            var tasks = new Task<uint>[MaxDegreeOfParallelism];

            for (uint i = 0; i < MaxDegreeOfParallelism; i++)
            {
                tasks[i] = Task.Factory.StartNew(
                    indexObject =>
                    {
                        var index = (uint)indexObject;
                        // A 16b PRNG is enough for this task and the xorshift one has suitable quality.
                        var random = new RandomXorshiftLfsr16 { State = (ushort)(randomSeed + index) };

                        uint inCircleCount = 0;

                        for (var j = 0; j < iterationsPerTask; j++)
                        {
                            uint a = random.NextUInt16();
                            uint b = random.NextUInt16();

                            // A bit of further parallelization can be exploited with SIMD to shave off some execution
                            // time. However, this needs so much resources on the hardware that the degree of
                            // parallelism needs to be lowered substantially (below 60).
                            //var randomNumbers = new uint[] { random.NextUInt16(), random.NextUInt16() };
                            //var products = Common.Numerics.SimdOperations.MultiplyVectors(randomNumbers, randomNumbers, 2);

                            if ((ulong)(a * a) + b * b <= ((uint)ushort.MaxValue * ushort.MaxValue))
                            //if ((ulong)products[0] + products[1] <= ((uint)ushort.MaxValue * ushort.MaxValue))
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

            memory.WriteUInt32(EstimatePi_InCircleCountSumUInt32Index, inCircleCountSum);
        }


        private readonly Random _random = new Random();

        public double EstimatePi(uint iterationsCount)
        {
            if (iterationsCount % MaxDegreeOfParallelism != 0)
            {
                throw new Exception($"The number of iterations must be divisible by {MaxDegreeOfParallelism}.");
            }

            var memory = new SimpleMemory(2);
            memory.WriteUInt32(EstimatePi_IteractionsCountUInt32Index, iterationsCount);
            memory.WriteUInt32(EstimatePi_RandomSeedUInt32Index, (uint)_random.Next(0, int.MaxValue));

            EstimatePi(memory);

            // This single calculation takes up too much space on the FPGA, since it needs fix point arithmetic, but
            // it doesn't take much time. So doing it on the host instead.
            return (double)memory.ReadInt32(EstimatePi_InCircleCountSumUInt32Index) / iterationsCount * 4;
        }
    }
}
