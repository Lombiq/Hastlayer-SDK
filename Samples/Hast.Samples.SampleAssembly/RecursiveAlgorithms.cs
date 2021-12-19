using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// Showcasing some simple recursive algorithms, since recursive method calls are also supported by Hastlayer. Do
    /// note however that recursive algorithms is not the best use-case of Hastlayer and should generally be avoided.
    ///
    /// Also see <c>RecursiveAlgorithmsSampleRunner</c> on what to configure to make this work.
    ///
    /// There is also an invocation counter for each of the methods, i.e. we'll be able to see how many times the
    /// methods were invoked. This is only interesting when debugging the hardware side: writing debug data to the
    /// memory like this can give you insights into how your code works on the hardware (this is even more useful if
    /// you also debug with the FPGA SDK and check the memory of the FPGA there). Note that naturally adding such
    /// memory operations makes the algorithm much slower.
    /// </summary>
    /// <remarks>
    /// <para>The private methods' names are prefixed so they can targeted for recursion configuration.</para>
    /// </remarks>
    public class RecursiveAlgorithms
    {
        private const int CalculateFibonacchiSeriesInputShortIndex = 0;
        private const int CalculateFibonacchiSeriesOutputUInt32Index = 0;
        private const int CalculateFibonacchiSeriesInvocationCounterUInt32Index = 1;
        private const int CalculateFactorialInputShortIndex = 0;
        private const int CalculateFactorialOutputUInt32Index = 0;
        private const int CalculateFactorialInvocationCounterUInt32Index = 1;

        public virtual void CalculateFibonacchiSeries(SimpleMemory memory)
        {
            memory.WriteUInt32(CalculateFibonacchiSeriesInvocationCounterUInt32Index, 1);

            var number = (short)memory.ReadInt32(CalculateFibonacchiSeriesInputShortIndex);
            memory.WriteUInt32(CalculateFibonacchiSeriesOutputUInt32Index, RecursivelyCalculateFibonacchiSeries(memory, number));
        }

        public uint CalculateFibonacchiSeries(short number, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null)
        {
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(2)
                : hastlayer.CreateMemory(configuration, 2);
            memory.WriteInt32(CalculateFibonacchiSeriesInputShortIndex, number);
            CalculateFibonacchiSeries(memory);
            return memory.ReadUInt32(CalculateFibonacchiSeriesOutputUInt32Index);
        }

        public virtual void CalculateFactorial(SimpleMemory memory)
        {
            memory.WriteUInt32(CalculateFactorialInvocationCounterUInt32Index, 1);

            var number = (short)memory.ReadInt32(CalculateFactorialInputShortIndex);
            memory.WriteUInt32(CalculateFactorialOutputUInt32Index, RecursivelyCalculateFactorial(memory, number));
        }

        public uint CalculateFactorial(short number, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null)
        {
            var memory = hastlayer is null
                ? SimpleMemory.CreateSoftwareMemory(2)
                : hastlayer.CreateMemory(configuration, 2);
            memory.WriteInt32(CalculateFactorialInputShortIndex, number);
            CalculateFactorial(memory);
            return memory.ReadUInt32(CalculateFactorialOutputUInt32Index);
        }

        // The return value should be a type with a bigger range than the input. Although we can use 64b numbers
        // internally we can't write the to memory yet so the input needs to be a short.
        private uint RecursivelyCalculateFibonacchiSeries(SimpleMemory memory, short number)
        {
            memory.WriteUInt32(
                CalculateFibonacchiSeriesInvocationCounterUInt32Index,
                memory.ReadUInt32(CalculateFibonacchiSeriesInvocationCounterUInt32Index) + 1);

            if (number is 0 or 1) return (uint)number;
            return RecursivelyCalculateFibonacchiSeries(memory, (short)(number - 2)) +
                RecursivelyCalculateFibonacchiSeries(memory, (short)(number - 1));
        }

        private uint RecursivelyCalculateFactorial(SimpleMemory memory, short number)
        {
            memory.WriteUInt32(
                CalculateFactorialInvocationCounterUInt32Index,
                memory.ReadUInt32(CalculateFactorialInvocationCounterUInt32Index) + 1);

            if (number == 0) return 1;
            return (uint)(number * RecursivelyCalculateFactorial(memory, (short)(number - 1)));
        }
    }
}
