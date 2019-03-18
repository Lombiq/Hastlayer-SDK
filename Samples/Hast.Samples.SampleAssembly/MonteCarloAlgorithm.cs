using System;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// Algorithm to find the weight and center of mass of a section of torus with varying density. Also see 
    /// <see cref="MonteCarloAlgorithmSampleRunner"/> on what to configure to make this work.
    ///  
    /// NOTE: this sample is not parallelized and thus not really suitable for Hastlayer. We'll rework it in the future.
    /// </summary>
    /// <remarks>
    /// Implementation taken from here: http://www.codeproject.com/Articles/767997/Parallelised-Monte-Carlo-Algorithms-sharp
    /// </remarks>
    public class MonteCarloAlgorithm
    {
        private const ushort Multiplier = 100;

        public const int MonteCarloAlgorithm_IterationsCountIndex = 0;
        public const int MonteCarloAlgorithm_WIndex = 1;
        public const int MonteCarloAlgorithm_XIndex = 2;
        public const int MonteCarloAlgorithm_YIndex = 3;
        public const int MonteCarloAlgorithm_ZIndex = 4;
        public const int MonteCarloAlgorithm_DWIndex = 5;
        public const int MonteCarloAlgorithm_DXIndex = 6;
        public const int MonteCarloAlgorithm_DYIndex = 7;
        public const int MonteCarloAlgorithm_DZIndex = 8;
        public const int MonteCarloAlgorithm_RandomNumbersStartIndex = 9;


        public virtual void CalculateTorusSectionValues(SimpleMemory memory)
        {
            int w, x, y, s, z, dw, dx, dy, dz, sw, swx, swy, swz, varw, varx, vary, varz, ss, volume;
            w = x = y = s = z = dw = dx = dy = dz = sw = swx = swy = swz = varw = varx = vary = varz = 0;

            // Rounded constant value instead of "0.2 * (Math.Exp(5.0) - Math.Exp(-5.0))". 
            // This is the interval of s to be random sampled. 
            ss = 29; 
            volume = 3 * 7 * ss; // Volume of the sampled region in x,y,s space.

            int iterationsCount = memory.ReadInt32(MonteCarloAlgorithm_IterationsCountIndex);

            int sumsw = 0;
            int sumswx = 0;
            int sumswy = 0;
            int sumswz = 0;
            int sumvarw = 0;
            int sumvarwx = 0;
            int sumvarwy = 0;
            int sumvarwz = 0;

            uint randomX = 0;
            uint randomY = 0;
            uint randomZ = 0;

            for (int i = 1; i <= iterationsCount; i++)
            {
                randomX = memory.ReadUInt32(MonteCarloAlgorithm_RandomNumbersStartIndex + i);
                randomY = memory.ReadUInt32(MonteCarloAlgorithm_RandomNumbersStartIndex + i + 1);
                randomZ = memory.ReadUInt32(MonteCarloAlgorithm_RandomNumbersStartIndex + i + 2);

                // Pick points randomly from the sampled region.
                x = checked((int)(Multiplier + randomX * 3 * Multiplier / 100));
              
                // The constant can't be specified properly inline as (since it can't be specified as a short, see:
                // http://stackoverflow.com/questions/8670511/how-to-specify-a-short-int-constant-without-casting)
                // it would cause an underflow and be cast to an ulong.
                short minusThree = -3;
                y = checked((int)(minusThree * Multiplier + randomY * 7 * Multiplier / 100));
                short thirteen = 13;
                s = checked((int)(thirteen + ss * (short)randomZ * Multiplier / 100));
                short two = 2;
                z = checked((int)(two * Multiplier * LogN(5 * s / Multiplier) / 10));

                int b = checked((int)(Sqrt((x * x) + (y * y)) - 3 * Multiplier));
                int a = checked((int)(((z * z) + (b * b)) / Multiplier));

                // Check if the selected points are inside the torus. 
                // If they are inside, add to the various cumulants.
                if (a < Multiplier)
                {
                    sw = checked(sw + Multiplier);
                    swx = checked(swx + x);
                    swy = checked(swy + y);
                    swz = checked(swz + z);
                    varw = Multiplier;
                    varx += (x * x) / Multiplier;
                    vary += (y * y) / Multiplier;
                    varz += (z * z) / Multiplier;
                }

                // Divide the values with the multiplier to return to the original numbers in every 1000th iteration. 
                // This way we can avoid overflows at the final computations, but we still get more precise values.
                if (i % 1000 == 0 || i == iterationsCount)
                {
                    sumsw = checked(sumsw + sw / Multiplier);
                    sumswx = checked(sumswx + swx / Multiplier);
                    sumswy = checked(sumswy + swy / Multiplier);
                    sumswz = checked(sumswz + swz / Multiplier);
                    sumvarw = Multiplier;
                    sumvarwx += varx / Multiplier;
                    sumvarwy += vary / Multiplier;
                    sumvarwz += varz / Multiplier;

                    sw = swx = swy = swz = varw = varx = vary = varz = 0;
                }
            }

            // Values of the integrals.
            memory.WriteUInt32(MonteCarloAlgorithm_WIndex, checked((uint)((uint)volume * (uint)sumsw / iterationsCount)));
            memory.WriteUInt32(MonteCarloAlgorithm_XIndex, checked((uint)((uint)volume * (uint)sumswx / iterationsCount)));
            memory.WriteUInt32(MonteCarloAlgorithm_YIndex, checked((uint)((uint)volume * (uint)sumswy / iterationsCount)));
            memory.WriteUInt32(MonteCarloAlgorithm_ZIndex, checked((uint)((uint)volume * (uint)sumswz / iterationsCount)));

            // Values of the error estimates.
            memory.WriteUInt32(MonteCarloAlgorithm_DWIndex, 
                checked((uint)(volume * Sqrt((int)((sumvarw / iterationsCount - Pow((sumsw / iterationsCount), 2)) / iterationsCount)))));
            memory.WriteUInt32(MonteCarloAlgorithm_DXIndex, 
                checked((uint)(volume * Sqrt((int)((sumvarwx / iterationsCount - Pow((sumswx / iterationsCount), 2)) / iterationsCount)))));
            memory.WriteUInt32(MonteCarloAlgorithm_DYIndex, 
                checked((uint)(volume * Sqrt((int)((sumvarwy / iterationsCount - Pow((sumswy / iterationsCount), 2)) / iterationsCount)))));
            memory.WriteUInt32(MonteCarloAlgorithm_DZIndex, 
                checked((uint)(volume * Sqrt((int)((sumvarwz / iterationsCount - Pow((sumswz / iterationsCount), 2)) / iterationsCount)))));
        }


        /// <summary>
        /// Estimates the square root of a number using the Babylonian method.
        /// </summary>
        /// <remarks>This is only needed because we don't support Math.Sqrt() yet.</remarks>
        /// <param name="value">The number we search the square root of.</param>
        /// <returns>Returns the square root of the number.</returns>
        private int Sqrt(int value)
        {
            if (value == 0) return 0;

            var current = 100;  // This is an initial value, where the algorithm starts the estimations.
            var previous = 0;

            // The algorithm is running until an acceptable punctuality is reached.
            while (current < previous - 10 || current > previous + 10)
            {
                previous = current;
                current = (previous + value / previous) / 2;
            }

            return current;
        }

        /// <summary>
        /// Calculates the natural based logarithm of a number.
        /// </summary>
        /// <param name="value">The number we search the natural based logarithm of.</param>
        /// <returns>Returns the natural based logarithm of the number.</returns>
        private int LogN(int value) => Log10(value) * 10000 / 4342; // 4342 is the value of Log(e) multiplied by 10000;

        /// <summary>
        /// Calculates the logarithm of a number.
        /// </summary>
        /// <param name="value">The number we search the logarithm of.</param>
        /// <returns>Returns the logarithm of the number.</returns>
        private int Log10(int value)
        {
            if (value >= 10000000) return 7;
            else if (value >= 1000000) return 6;
            else if (value >= 100000) return 5;
            else if (value >= 10000) return 4;
            else if (value >= 1000) return 3;
            else if (value >= 100) return 2;
            else if (value >= 10) return 1;
            else return 0;
        }

        /// <summary>
        /// Calculates the power of a number.
        /// </summary>
        /// <param name="value">The base value.</param>
        /// <param name="power">The power of the calculation.</param>
        /// <returns>Returns the number raised to the power.</returns>
        private int Pow(int value, int power)
        {
            var baseValue = value;

            for (int i = 0; i < power - 1; i++) baseValue *= value;

            return baseValue;
        }
    }


    public static class MonteCarloAlgorithmExtensions
    {
        private static Random _random = new Random();


        /// <summary>
        /// Algorithm to find the weight and center of mass of a section of torus with varying density.
        /// </summary>
        /// <param name="iterationsCount">The number of iterations the algorithm uses for calculations.</param>
        /// <returns>
        /// Returns the weight and center of mass of a section of torus with varying density in the form of a
        /// <see cref="MonteCarloResult"/> object.
        /// </returns>
        public static MonteCarloResult CalculateTorusSectionValues(this MonteCarloAlgorithm monteCarloAlgorithm, int iterationsCount)
        {
            var simpleMemory = CreateSimpleMemory(iterationsCount);

            monteCarloAlgorithm.CalculateTorusSectionValues(simpleMemory);

            return GetResult(simpleMemory);
        }


        /// <summary>
        /// Creates a <see cref="SimpleMemory"/> object filled with the input values.
        /// </summary>
        /// <param name="iterationsCount">The number of iterations the algorithm uses for calculations.</param>
        /// <returns>Returns a <see cref="SimpleMemory"/> object containing the input values.</returns>
        private static SimpleMemory CreateSimpleMemory(int iterationsCount)
        {
            var simpleMemory = new SimpleMemory(10 + iterationsCount * 3);

            simpleMemory.WriteInt32(MonteCarloAlgorithm.MonteCarloAlgorithm_IterationsCountIndex, iterationsCount);

            for (int i = 0; i < iterationsCount * 3; i++)
            {
                simpleMemory.WriteUInt32(MonteCarloAlgorithm.MonteCarloAlgorithm_RandomNumbersStartIndex + i, (uint)(_random.Next(101)));
            }

            return simpleMemory;
        }

        /// <summary>
        /// Calculates the weight and center of mass of a section of torus with varying density from a <see
        /// cref="SimpleMemory"/> object.
        /// </summary>
        /// <param name="simpleMemory">The <see cref="SimpleMemory"/> object that contains the result.</param>
        /// <returns>
        /// Returns the weight and center of mass of a section of torus with varying density in the form of a
        /// <see cref="MonteCarloResult"/> object.
        /// </returns>
        private static MonteCarloResult GetResult(SimpleMemory simpleMemory)
        {
            return new MonteCarloResult
            {
                W = simpleMemory.ReadUInt32(MonteCarloAlgorithm.MonteCarloAlgorithm_WIndex),
                X = simpleMemory.ReadUInt32(MonteCarloAlgorithm.MonteCarloAlgorithm_XIndex),
                Y = simpleMemory.ReadUInt32(MonteCarloAlgorithm.MonteCarloAlgorithm_YIndex),
                Z = simpleMemory.ReadUInt32(MonteCarloAlgorithm.MonteCarloAlgorithm_ZIndex),
                DW = simpleMemory.ReadUInt32(MonteCarloAlgorithm.MonteCarloAlgorithm_DWIndex),
                DX = simpleMemory.ReadUInt32(MonteCarloAlgorithm.MonteCarloAlgorithm_DXIndex),
                DY = simpleMemory.ReadUInt32(MonteCarloAlgorithm.MonteCarloAlgorithm_DYIndex),
                DZ = simpleMemory.ReadUInt32(MonteCarloAlgorithm.MonteCarloAlgorithm_DZIndex)
            };
        }
    }
}
