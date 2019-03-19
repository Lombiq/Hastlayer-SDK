using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Hast.Transformer.Abstractions.SimpleMemory;

namespace Hast.Samples.SampleAssembly
{
    /// <summary>
    /// Example for a SimpleMemory-using algorithm. Also see <see cref="PrimeCalculatorSampleRunner"/> on what to 
    /// configure to make this work.
    /// </summary>
    public class PrimeCalculator
    {
        // It's good to have externally interesting cell indices in constants like this, so they can be used from wrappers 
        // like below. Note the Hungarian notation-like prefixes. It's unfortunate but we need them here for clarity.
        public const int IsPrimeNumber_InputUInt32Index = 0;
        public const int IsPrimeNumber_OutputBooleanIndex = 0;
        public const int ArePrimeNumbers_InputUInt32CountIndex = 0;
        public const int ArePrimeNumbers_InputUInt32sStartIndex = 1;
        public const int ArePrimeNumbers_OutputBooleansStartIndex = 1;

        public const int MaxDegreeOfParallelism = 30;


        /// <summary>
        /// Calculates whether a number is prime.
        /// </summary>
        /// <remarks>
        /// This demonstrates a simple hardware entry point. Note that the entry point of SimpleMemory-using algorithms 
        /// should be void methods having a single <see cref="SimpleMemory"/> argument. 
        /// </remarks>
        /// <param name="memory">The <see cref="SimpleMemory"/> object representing the accessible memory space.</param>
        public virtual void IsPrimeNumber(SimpleMemory memory)
        {
            // Reading out the input parameter.
            var number = memory.ReadUInt32(IsPrimeNumber_InputUInt32Index);
            // Writing back the output.
            memory.WriteBoolean(IsPrimeNumber_OutputBooleanIndex, IsPrimeNumberInternal(number));
        }

        /// <summary>
        /// Calculates whether the number is prime, in an async way.
        /// </summary>
        /// <remarks>
        /// For efficient parallel execution with multiple connected FPGA boards you can make a non-parallelized hardware 
        /// entry point method async like this.
        /// </remarks>
        public virtual Task IsPrimeNumberAsync(SimpleMemory memory)
        {
            IsPrimeNumber(memory);

            // In .NET <4.6 Task.FromResult(true) can be used too.
            return Task.CompletedTask;
        }

        /// <summary>
        /// Calculates for multiple numbers whether they're primes.
        /// </summary>
        /// <remarks>
        /// A simple demonstration on how you can manage an array of inputs and outputs.
        /// </remarks>
        public virtual void ArePrimeNumbers(SimpleMemory memory)
        {
            // We need this information explicitly as we can't store arrays directly in memory.
            uint numberCount = memory.ReadUInt32(ArePrimeNumbers_InputUInt32CountIndex);

            for (int i = 0; i < numberCount; i++)
            {
                uint number = memory.ReadUInt32(ArePrimeNumbers_InputUInt32sStartIndex + i);
                memory.WriteBoolean(ArePrimeNumbers_OutputBooleansStartIndex + i, IsPrimeNumberInternal(number));
            }
        }

        /// <summary>
        /// Calculates for multiple numbers whether they're primes, in a parallelized way.
        /// </summary>
        /// <remarks>
        /// This demonstrates how you can write parallelized code that Hastlayer will process and turn into hardware-level
        /// parallelization: the Tasks' bodies will be copied in hardware as many times as many Tasks you start; thus, 
        /// the actual level of parallelism you get on the hardware corresponds to the number of Tasks, not the number
        /// of CPU cores.
        /// </remarks>
        public virtual void ParallelizedArePrimeNumbers(SimpleMemory memory)
        {
            // We need this information explicitly as we can't store arrays directly in memory.
            uint numberCount = memory.ReadUInt32(ArePrimeNumbers_InputUInt32CountIndex);

            // At the moment Hastlayer only supports a fixed degree of parallelism so we need to pad the input array
            // if necessary, see PrimeCalculatorExtensions.
            var tasks = new Task<bool>[MaxDegreeOfParallelism];
            int i = 0;
            while (i < numberCount)
            {
                // NOTE: multi-cell read/write operations are commented out at the moment. These will be put back once
                // that's completely supported.

                // Note that you can read/write multiple values from/to SimpleMemory at once. On capable platforms this 
                // will correspond to batched read/writes, which are much faster than doing them one by one.
                //var numbers = memory.ReadUInt32(ArePrimeNumbers_InputUInt32sStartIndex + i, MaxDegreeOfParallelism);

                for (int m = 0; m < MaxDegreeOfParallelism; m++)
                {
                    var currentNumber = memory.ReadUInt32(ArePrimeNumbers_InputUInt32sStartIndex + i + m);

                    // Note that you can just call (thread-safe) methods from inside Tasks as usual. In hardware those
                    // invoked methods will be copied together with the Tasks' bodies too.
                    tasks[m] = Task.Factory.StartNew(
                        numberObject => IsPrimeNumberInternal((uint)numberObject),
                        currentNumber);
                    //tasks[m] = Task.Factory.StartNew(
                    //    numberObject => IsPrimeNumberInternal((uint)numberObject),
                    //    numbers[m]);
                }

                // Hastlayer doesn't support async code at the moment since ILSpy doesn't handle the new Roslyn-compiled
                // code. See: https://github.com/icsharpcode/ILSpy/issues/502
                Task.WhenAll(tasks).Wait();

                //var results = new bool[MaxDegreeOfParallelism];
                for (int m = 0; m < MaxDegreeOfParallelism; m++)
                {
                    memory.WriteBoolean(ArePrimeNumbers_OutputBooleansStartIndex + i + m, tasks[m].Result);
                    //results[m] = tasks[m].Result;
                }

                //memory.WriteBoolean(ArePrimeNumbers_OutputBooleansStartIndex + i, results);

                i += MaxDegreeOfParallelism;
            }
        }


        /// <summary>
        /// Internal implementation of prime number checking. This is here so we can use it simpler from two methods.
        /// Because when you want to pass data between methods you can freely use supported types as arguments, you 
        /// don't need to pass data through SimpleMemory.
        /// </summary>
        /// <remarks>
        /// Note the usage of the AggressiveInlining option, which tells Hastlayer that the method can be inlined, i.e.
        /// basically its implementation can be copied over to where it is called. This is a performance optimization:
        /// this way the overhead of method calls is eliminated and thus execution will be faster (very useful if you 
        /// have small methods called frequently, like this one from ParallelizedArePrimeNumbers()) but the hardware 
        /// implementation will most possibly use more resources from the FPGA (though not always; since the method 
        /// invocation was cut out it can even utilize less resources, check for your program).
        /// 
        /// In the case of the PrimeCalculator sample inlining this method had the following effects:
        /// - Execution time went down: ArePrimeNumbers() took 1151ms without and 1069ms with inlining (-7%), 
        ///   ParallelizedArePrimeNumbers() took 64ms without and 60ms with inlining (-6%).
        /// - Resource usage went up from 77% to 79% (for the most utilized resource type on the FPGA, the one that 
        ///   limits further use).
        ///   
        /// WARNING: be sure not to overdo inlining, because just inlining everything (especially if inlined methods
        /// also call inlined methods...) can quickly create giant hardware implementations that won't be just most
        /// possibly too big for the FPGA but also very slow to transform.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsPrimeNumberInternal(uint number)
        {
            uint factor = number / 2;
            //uint factor = Math.Sqrt(number); // Math.Sqrt() can't be processed because it's not a managed method.

            // i mustn't be an int, because due to the mismatch with the uint number and factor all of these would be
            // cast to long by the C# compiler, resulting in much slower hardware.
            uint i = 2;
            while (i <= factor && number % i != 0)
            {
                i++;
            }

            return i == factor + 1;
        }
    }


    /// <summary>
    /// Extension methods so the SimpleMemory-using PrimeCalculator is easier to consume.
    /// </summary>
    public static class PrimeCalculatorExtensions
    {
        public static bool IsPrimeNumber(this PrimeCalculator primeCalculator, uint number)
        {
            return RunIsPrimeNumber(number, memory => Task.Run(() => primeCalculator.IsPrimeNumber(memory))).Result;
        }

        public static Task<bool> IsPrimeNumberAsync(this PrimeCalculator primeCalculator, uint number)
        {
            return RunIsPrimeNumber(number, memory => primeCalculator.IsPrimeNumberAsync(memory));
        }

        public static bool[] ArePrimeNumbers(this PrimeCalculator primeCalculator, uint[] numbers)
        {
            return RunArePrimeNumbersMethod(numbers, memory => primeCalculator.ArePrimeNumbers(memory));
        }

        public static bool[] ParallelizedArePrimeNumbers(this PrimeCalculator primeCalculator, uint[] numbers)
        {
            var results = RunArePrimeNumbersMethod(
                numbers.PadToMultipleOf(PrimeCalculator.MaxDegreeOfParallelism),
                memory => primeCalculator.ParallelizedArePrimeNumbers(memory));

            // The result might be longer than the input due to padding.
            return results.CutToLength(numbers.Length);
        }

        private static async Task<bool> RunIsPrimeNumber(uint number, Func<SimpleMemory, Task> methodRunner)
        {
            // One memory cell is enough for data exchange.
            var memory = new SimpleMemory(1);
            memory.WriteUInt32(PrimeCalculator.IsPrimeNumber_InputUInt32Index, number);

            await methodRunner(memory);

            return memory.ReadBoolean(PrimeCalculator.IsPrimeNumber_OutputBooleanIndex);
        }

        private static bool[] RunArePrimeNumbersMethod(uint[] numbers, Action<SimpleMemory> methodRunner)
        {
            // We need to allocate more memory cells, enough for all the inputs and outputs.
            var memory = new SimpleMemory(numbers.Length + 1);

            memory.WriteUInt32(PrimeCalculator.ArePrimeNumbers_InputUInt32CountIndex, (uint)numbers.Length);
            for (int i = 0; i < numbers.Length; i++)
            {
                memory.WriteUInt32(PrimeCalculator.ArePrimeNumbers_InputUInt32sStartIndex + i, numbers[i]);
            }


            methodRunner(memory);


            var output = new bool[numbers.Length];
            for (int i = 0; i < numbers.Length; i++)
            {
                output[i] = memory.ReadBoolean(PrimeCalculator.ArePrimeNumbers_OutputBooleansStartIndex + i);
            }
            return output;
        }
    }
}
