using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// Showcasing some simple recursive algorithms, since recursive method calls are also supported by Hastlayer. Do
    /// note however that recursive algorithms is not the best use-case of Hastlayer and should generally be avoided.
    /// 
    /// There is also an invocation counter for each of the methods, i.e. we'll be able to see how many times the methods
    /// were invoked. This is only interesting when debugging the hardware side: writing debug data to the memory like
    /// this can give you insights into how your code works on the hardware (this is even more useful if you also debug
    /// with the FPGA SDK and check the memory of the FPGA there). Note that naturally adding such memory operations
    /// makes the algorithm much slower.
    /// </summary>
    /// <remarks>
    /// The private methods' names are prefixed so they can targeted for recursion configuration.
    /// </remarks>
    public class RecursiveAlgorithms
    {
        public const int CalculateFibonacchiSeries_InputShortIndex = 0;
        public const int CalculateFibonacchiSeries_OutputUInt32Index = 0;
        public const int CalculateFibonacchiSeries_InvocationCounterUInt32Index = 1;
        public const int CalculateFactorial_InputShortIndex = 0;
        public const int CalculateFactorial_OutputUInt32Index = 0;
        public const int CalculateFactorial_InvocationCounterUInt32Index = 1;


        public virtual void CalculateFibonacchiSeries(SimpleMemory memory)
        {
            memory.WriteUInt32(CalculateFibonacchiSeries_InvocationCounterUInt32Index, 1);

            var number = (short)memory.ReadInt32(CalculateFibonacchiSeries_InputShortIndex);
            memory.WriteUInt32(CalculateFibonacchiSeries_OutputUInt32Index, RecursivelyCalculateFibonacchiSeries(memory, number));
        }

        public virtual void CalculateFactorial(SimpleMemory memory)
        {
            memory.WriteUInt32(CalculateFactorial_InvocationCounterUInt32Index, 1);

            var number = (short)memory.ReadInt32(CalculateFactorial_InputShortIndex);
            memory.WriteUInt32(CalculateFactorial_OutputUInt32Index, RecursivelyCalculateFactorial(memory, number));
        }


        // The return value should be a type with a bigger range than the input. Although we can use 64b numbers
        // internally we can't write the to memory yet so the input needs to be a short.
        private uint RecursivelyCalculateFibonacchiSeries(SimpleMemory memory, short number)
        {
            memory.WriteUInt32(
                CalculateFibonacchiSeries_InvocationCounterUInt32Index, 
                memory.ReadUInt32(CalculateFibonacchiSeries_InvocationCounterUInt32Index) + 1);

            if (number == 0 || number == 1) return (uint)number;
            return RecursivelyCalculateFibonacchiSeries(memory, (short)(number - 2)) + RecursivelyCalculateFibonacchiSeries(memory, (short)(number - 1));
        }

        private uint RecursivelyCalculateFactorial(SimpleMemory memory, short number)
        {
            memory.WriteUInt32(
                CalculateFactorial_InvocationCounterUInt32Index,
                memory.ReadUInt32(CalculateFactorial_InvocationCounterUInt32Index) + 1);

            if (number == 0) return 1;
            return (uint)(number * RecursivelyCalculateFactorial(memory, (short)(number - 1)));
        }
    }


    public static class RecursiveAlgorithmsExtensions
    {
        public static uint CalculateFibonacchiSeries(this RecursiveAlgorithms recursiveAlgorithms, short number)
        {
            var memory = new SimpleMemory(2);
            memory.WriteInt32(RecursiveAlgorithms.CalculateFibonacchiSeries_InputShortIndex, number);
            recursiveAlgorithms.CalculateFibonacchiSeries(memory);
            return memory.ReadUInt32(RecursiveAlgorithms.CalculateFibonacchiSeries_OutputUInt32Index);
        }

        public static uint CalculateFactorial(this RecursiveAlgorithms recursiveAlgorithms, short number)
        {
            var memory = new SimpleMemory(2);
            memory.WriteInt32(RecursiveAlgorithms.CalculateFactorial_InputShortIndex, number);
            recursiveAlgorithms.CalculateFactorial(memory);
            return memory.ReadUInt32(RecursiveAlgorithms.CalculateFactorial_OutputUInt32Index);
        }
    }
}
