using Hast.Algorithms;
using Hast.Transformer.Abstractions.SimpleMemory;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Samples.SampleAssembly
{
    //public static class TrivialPiCalculator
    //{
    //    public const int ScaleFactor = 1500000000;


    //    public static Fix64 Calculate(int iterations)
    //    {
    //        int inCircle = 0;
    //        var random = new PrngMWC64X();
    //        for (int i = 0; i < iterations; i++)
    //        {
    //            //var a = new Fix64(random.Next(0, int.MaxValue)) / new Fix64(int.MaxValue);
    //            //var b = new Fix64(random.Next(0, int.MaxValue)) / new Fix64(int.MaxValue);

    //            var a = (Fix64)random.NextUInt32() / (Fix64)int.MaxValue;
    //            var b = (Fix64)random.NextUInt32() / (Fix64)int.MaxValue;

    //            //var a = (Fix64)random.NextUInt32() / (Fix64)(long)uint.MaxValue;
    //            //var b = (Fix64)random.NextUInt32() / (Fix64)int.MaxValue;

    //            if (a * a + b * b <= (Fix64)1)
    //            {
    //                inCircle++;
    //            }
    //        }

    //        // Kb. 392838
    //        return (Fix64)inCircle / (Fix64)iterations * (Fix64)4;
    //    }
    //}

    //public static class FastPiCalculator
    //{
    //    public const int ScaleFactor = 1000000;


    //    public static double Calculate(int iterations)
    //    {
    //        var procCount = Environment.ProcessorCount;
    //        if (iterations % procCount != 0)
    //        {
    //            throw new ArgumentException("Must be a multiple of Environment.ProcessorCount", "iterations");
    //        }

    //        // Distribute iterations evenly across processors
    //        var iterPerProc = iterations / procCount;

    //        // One array slot per processor
    //        var inCircleLocal = new int[procCount];
    //        var tasks = new Task[procCount];
    //        for (var proc = 0; proc < procCount; proc++)
    //        {
    //            var procIndex = proc; // Helper for closure

    //            // Start one task per processor
    //            tasks[proc] = Task.Run(() =>
    //            {
    //                var inCircleLocalCounter = 0;
    //                var random = new PrngMWC64X((uint)procIndex);

    //                for (var index = 0; index < iterPerProc; index++)
    //                {
    //                    double a, b;
    //                    a = random.NextUInt32() % ScaleFactor;
    //                    b = random.NextUInt32() % ScaleFactor;

    //                    if (a * a + b * b <= ScaleFactor)
    //                    {
    //                        inCircleLocalCounter++;
    //                    }
    //                }

    //                inCircleLocal[procIndex] = inCircleLocalCounter;
    //            });
    //        }

    //        Task.WaitAll(tasks);

    //        // 125660303 körül?
    //        var inCircle = inCircleLocal.Sum();
    //        return ((double)inCircle / iterations) * 4;
    //    }
    //}


    /// <summary>
    /// Algorithm to calculate Pi with a random <see href="https://en.wikipedia.org/wiki/Monte_Carlo_method"> Monte
    /// Carlo method</see> in a parallelized manner. For an overview  of the idea see <see
    /// href="https://www.coursera.org/lecture/parprog1/monte-carlo-method-to-estimate-pi-Zgm76">this video</see>;
    /// <see href="http://www.software-architects.com/devblog/2014/09/22/C-Parallel-and-Async-Programming"/>this
    /// blogpost's implementation</see> was used as an inspiration too. Also see <see
    /// cref="MonteCarloAlgorithmSampleRunner"/> on what to configure to make this work.
    /// </summary>
    /// <remarks>
    /// This uses about 80% of the LUTs on a Nexys A7 so nothing else will fit.
    /// Implementation taken from here: http://www.codeproject.com/Articles/767997/Parallelised-Monte-Carlo-Algorithms-sharp
    /// </remarks>
    public class MonteCarloPiEstimator
    {
        public const int MaxDegreeOfParallelism = 5;
        public const int EstimatePi_IteractionsCountUInt32Index = 0;
        public const int EstimatePi_RandomSeedUInt32Index = 1;
        public const int EstimatePi_PiEstimateFix64StartIndex = 0;


        public virtual void EstimatePi(SimpleMemory memory)
        {
            var iterationsCount = memory.ReadUInt32(EstimatePi_IteractionsCountUInt32Index);
            var randomSeed = memory.ReadUInt32(EstimatePi_RandomSeedUInt32Index);

            var iterationsPerTask = iterationsCount / MaxDegreeOfParallelism;
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
                            // While we can use floating point numbers to represent fractions as well it's more
                            // efficient to compute with fixed point.
                            var x = (Fix64)random.NextUInt32() / (Fix64)int.MaxValue;
                            var y = (Fix64)random.NextUInt32() / (Fix64)int.MaxValue;

                            if (x * x + y * y <= (Fix64)1)
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

            var piEstimate = (Fix64)inCircleCountSum / (Fix64)iterationsCount * (Fix64)4;
            var piEstimateIntegers = piEstimate.ToIntegers();
            memory.WriteInt32(EstimatePi_PiEstimateFix64StartIndex, piEstimateIntegers[0]);
            memory.WriteInt32(EstimatePi_PiEstimateFix64StartIndex + 1, piEstimateIntegers[1]);
        }
    }


    public static class MonteCarloPiEstimatorExtensions
    {
        private static Random _random = new Random();


        public static Fix64 EstimatePi(this MonteCarloPiEstimator piEstimator, uint iterationsCount)
        {
            if (iterationsCount % MonteCarloPiEstimator.MaxDegreeOfParallelism != 0)
            {
                throw new Exception($"The number of iterations must be divisible by {MonteCarloPiEstimator.MaxDegreeOfParallelism}.");
            }

            var memory = new SimpleMemory(2);
            memory.WriteUInt32(MonteCarloPiEstimator.EstimatePi_IteractionsCountUInt32Index, iterationsCount);
            memory.WriteUInt32(MonteCarloPiEstimator.EstimatePi_RandomSeedUInt32Index, (uint)_random.Next(0, int.MaxValue));

            piEstimator.EstimatePi(memory);

            return Fix64.FromRawInts(new[]
            {
                memory.ReadInt32(MonteCarloPiEstimator.EstimatePi_PiEstimateFix64StartIndex),
                memory.ReadInt32(MonteCarloPiEstimator.EstimatePi_PiEstimateFix64StartIndex + 1)
            });
        }
    }
}
