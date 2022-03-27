using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Hast.Samples.SampleAssembly;

/// <summary>
/// Example for a SimpleMemory-using algorithm. Also see <c>PrimeCalculatorSampleRunner</c> on what to configure to
/// make this work.
/// </summary>
[SuppressMessage(
    "Minor Code Smell",
    "S4136:Method overloads should be grouped together",
    Justification = "Helpers are moved together to a separate region")]
public class PrimeCalculator
{
    // It's good to have common cell indices in constants like this, so they can be used from multiple methods
    // like below. Note the Hungarian notation-like prefixes. These add some clarity but are not mandatory.
    private const int IsPrimeNumberInputUInt32Index = 0;
    private const int IsPrimeNumberOutputBooleanIndex = 0;
    private const int ArePrimeNumbersInputUInt32CountIndex = 0;
    private const int ArePrimeNumbersInputUInt32SStartIndex = 1;
    private const int ArePrimeNumbersOutputBooleansStartIndex = 1;

    private const int MaxDegreeOfParallelism = 30;

    // Note that below there are method pairs: one method with a SimpleMemory parameter and one with built-in types.
    // The hardware entry points, i.e. the methods actually called from the host PC on the hardware device will be
    // the ones with SimpleMemory: do every accelerated processing in there. The other methods are wrappers so you
    // can execute the inner methods more easily but they won't be converted to hardware.

    /// <summary>
    /// Calculates whether a number is prime.
    /// </summary>
    /// <param name="memory">The <see cref="SimpleMemory"/> object representing the accessible memory space.</param>
    /// <remarks>
    /// <para>
    /// This demonstrates a simple hardware entry point. Note that the entry point of SimpleMemory-using algorithms
    /// should be void methods having a single <see cref="SimpleMemory"/> argument. You can find the corresponding
    /// wrapper method below as IsPrimeNumber(uint number).
    ///
    /// Note that hardware entry points need to be public and virtual, and they mustn't have any other parameters
    /// than a single SimpleMemory one.
    /// </para>
    /// </remarks>
    public virtual void IsPrimeNumberSync(SimpleMemory memory)
    {
        // Reading out the input parameter.
        var number = memory.ReadUInt32(IsPrimeNumberInputUInt32Index);
        // Writing back the output.
        memory.WriteBoolean(IsPrimeNumberOutputBooleanIndex, IsPrimeNumberInternal(number));
    }

    /// <summary>
    /// Calculates whether the number is prime, in an async way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For efficient parallel execution with multiple connected FPGA boards you can make a non-parallelized
    /// hardware entry point method async like this.
    /// </para>
    /// </remarks>
    public virtual Task IsPrimeNumberAsync(SimpleMemory memory)
    {
        IsPrimeNumberSync(memory);

        // In .NET <4.6 Task.FromResult(true) can be used too.
        return Task.CompletedTask;
    }

    /// <summary>
    /// Calculates for multiple numbers whether they're primes.
    /// </summary>
    /// <remarks>
    /// <para>A simple demonstration on how you can manage an array of inputs and outputs.</para>
    /// </remarks>
    public virtual void ArePrimeNumbers(SimpleMemory memory)
    {
        // We need this information explicitly as we can't store arrays directly in memory.
        uint numberCount = memory.ReadUInt32(ArePrimeNumbersInputUInt32CountIndex);

        for (int i = 0; i < numberCount; i++)
        {
            uint number = memory.ReadUInt32(ArePrimeNumbersInputUInt32SStartIndex + i);
            memory.WriteBoolean(ArePrimeNumbersOutputBooleansStartIndex + i, IsPrimeNumberInternal(number));
        }
    }

    /// <summary>
    /// Calculates for multiple numbers whether they're primes, in a parallelized way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This demonstrates how you can write parallelized code that Hastlayer will process and turn into
    /// hardware-level parallelization: the Tasks' bodies will be copied in hardware as many times as many Tasks you
    /// start; thus, the actual level of parallelism you get on the hardware corresponds to the number of Tasks, not
    /// the number of CPU cores.
    /// </para>
    /// </remarks>
    public virtual void ParallelizedArePrimeNumbers(SimpleMemory memory)
    {
        // We need this information explicitly as we can't store arrays directly in memory.
        uint numberCount = memory.ReadUInt32(ArePrimeNumbersInputUInt32CountIndex);

        // At the moment Hastlayer only supports a fixed degree of parallelism so we need to pad the input array
        // if necessary, see PrimeCalculatorExtensions.
        var tasks = new Task<bool>[MaxDegreeOfParallelism];
        int i = 0;
        while (i < numberCount)
        {
            for (int m = 0; m < MaxDegreeOfParallelism; m++)
            {
                var currentNumber = memory.ReadUInt32(ArePrimeNumbersInputUInt32SStartIndex + i + m);

                // Note that you can just call (thread-safe) methods from inside Tasks as usual. In hardware those
                // invoked methods will be copied together with the Tasks' bodies too.
                tasks[m] = Task.Factory.StartNew(
                    numberObject => IsPrimeNumberInternal((uint)numberObject),
                    currentNumber);
            }

            // Hastlayer doesn't support async code at the moment since ILSpy doesn't handle the new Roslyn-compiled
            // code. See: https://github.com/icsharpcode/ILSpy/issues/502
            Task.WhenAll(tasks).Wait();

            for (int m = 0; m < MaxDegreeOfParallelism; m++)
            {
                memory.WriteBoolean(ArePrimeNumbersOutputBooleansStartIndex + i + m, tasks[m].Result);
            }

            i += MaxDegreeOfParallelism;
        }
    }

    /// <summary>
    /// Internal implementation of prime number checking. This is here so we can use it simpler from two methods.
    /// Because when you want to pass data between methods you can freely use supported types as arguments, you
    /// don't need to pass data through SimpleMemory.
    /// </summary>
    /// <remarks>
    /// <para>
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
    /// Methods can also be inlined with the help of the
    /// <c>TransformerConfiguration().AddAdditionalInlinableMethod&lt;T&gt;()</c> configuration.
    ///
    /// WARNING: be sure not to overdo inlining, because just inlining everything (especially if inlined methods
    /// also call inlined methods...) can quickly create giant hardware implementations that won't be just most
    /// possibly too big for the FPGA but also very slow to transform.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsPrimeNumberInternal(uint number)
    {
        uint factor = number / 2;
        //// uint factor = Math.Sqrt(number); // Math.Sqrt() can't be processed because it's not a managed method.

        // Here the i variable mustn't be an int, because due to the mismatch with the uint number and factor all of
        // these would be cast to long by the C# compiler, resulting in much slower hardware.
        uint i = 2;
        while (i <= factor && number % i != 0)
        {
            i++;
        }

        return i == factor + 1;
    }

    // Below are the methods that make the SimpleMemory-using methods easier to consume from the outside. These
    // won't be transformed into hardware since they're automatically omitted by Hastlayer (because they're not
    // hardware entry point members, nor are they used by any other transformed member). Thus you can do anything
    // in them that is not Hastlayer-compatible.

    #region Helpers
    public bool IsPrimeNumberSync(uint number, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null) =>
        RunIsPrimeNumberAsync(number, memory => Task.Run(() => IsPrimeNumberSync(memory)), hastlayer, configuration).Result;

    public Task<bool> IsPrimeNumberAsync(uint number, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null) =>
        RunIsPrimeNumberAsync(number, memory => IsPrimeNumberAsync(memory), hastlayer, configuration);

    public bool[] ArePrimeNumbers(uint[] numbers, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null) =>
        RunArePrimeNumbersMethod(numbers, memory => ArePrimeNumbers(memory), hastlayer, configuration);

    public bool[] ParallelizedArePrimeNumbers(uint[] numbers, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null)
    {
        var results = RunArePrimeNumbersMethod(
            numbers.PadToMultipleOf(MaxDegreeOfParallelism),
            ParallelizedArePrimeNumbers,
            hastlayer,
            configuration);

        // The result might be longer than the input due to padding.
        return results.CutToLength(numbers.Length);
    }

    private async Task<bool> RunIsPrimeNumberAsync(
        uint number,
        Func<SimpleMemory, Task> methodRunner,
        IHastlayer hastlayer = null,
        IHardwareGenerationConfiguration configuration = null)
    {
        // One memory cell is enough for data exchange.
        var memory = hastlayer is null
            ? SimpleMemory.CreateSoftwareMemory(1)
            : hastlayer.CreateMemory(configuration, 1);
        memory.WriteUInt32(IsPrimeNumberInputUInt32Index, number);

        await methodRunner(memory);

        return memory.ReadBoolean(IsPrimeNumberOutputBooleanIndex);
    }

    private bool[] RunArePrimeNumbersMethod(
        uint[] numbers,
        Action<SimpleMemory> methodRunner,
        IHastlayer hastlayer = null,
        IHardwareGenerationConfiguration configuration = null)
    {
        // We need to allocate more memory cells, enough for all the inputs and outputs.
        var memory = hastlayer is null
            ? SimpleMemory.CreateSoftwareMemory(numbers.Length + 1)
            : hastlayer.CreateMemory(configuration, numbers.Length + 1);

        memory.WriteUInt32(ArePrimeNumbersInputUInt32CountIndex, (uint)numbers.Length);
        for (int i = 0; i < numbers.Length; i++)
        {
            memory.WriteUInt32(ArePrimeNumbersInputUInt32SStartIndex + i, numbers[i]);
        }

        methodRunner(memory);

        var output = new bool[numbers.Length];
        for (int i = 0; i < numbers.Length; i++)
        {
            output[i] = memory.ReadBoolean(ArePrimeNumbersOutputBooleansStartIndex + i);
        }

        return output;
    }

    #endregion
}
