using System;
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
                for (int m = 0; m < MaxDegreeOfParallelism; m++)
                {
                    var currentNumber = memory.ReadUInt32(ArePrimeNumbers_InputUInt32sStartIndex + i + m);

                    tasks[m] = Task.Factory.StartNew(
                        numberObject =>
                        {
                            // This is a copy of the body of IsPrimeNumberInternal(). We could also call that method
                            // from this lambda but it's more efficient to just do it directly, not adding indirection.
                            var number = (uint)numberObject;
                            uint factor = number / 2;

                            for (uint x = 2; x <= factor; x++)
                            {
                                if ((number % x) == 0) return false;
                            }

                            return true;
                        },
                        currentNumber);
                }

                // Hastlayer doesn't support async code at the moment since ILSpy doesn't handle the new Roslyn-compiled
                // code. See: https://github.com/icsharpcode/ILSpy/issues/502
                Task.WhenAll(tasks).Wait();

                for (int m = 0; m < MaxDegreeOfParallelism; m++)
                {
                    memory.WriteBoolean(ArePrimeNumbers_OutputBooleansStartIndex + i + m, tasks[m].Result);
                }

                i += MaxDegreeOfParallelism;
            }
        }


        /// <summary>
        /// Internal implementation of prime number checking. This is here so we can use it simpler from two methods.
        /// Because when you want to pass data between methods you can freely use supported types as arguments, you 
        /// don't need to pass data through SimpleMemory.
        /// </summary>
        private bool IsPrimeNumberInternal(uint number)
        {
            uint factor = number / 2;
            //uint factor = Math.Sqrt(number); // Math.Sqrt() can't be processed because it's not a managed method.

            for (uint i = 2; i <= factor; i++)
            {
                if ((number % i) == 0) return false;
            }

            return true;
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
