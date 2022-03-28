using Hast.Common.Numerics;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Hast.Samples.SampleAssembly;

public enum SimdOperation
{
    Add,
    Subtract,
    Multiply,
    Divide,
}

/// <summary>
/// Sample to showcase SIMD (Simple Instruction Multiple Data) processing usage, i.e. operations executed in parallel on
/// multiple elements of vectors. Also see <c>SimdCalculatorSampleRunner</c> on what to configure to make this work.
/// </summary>
/// <remarks>
/// <para>
/// <c>System.Numerics.Vectors</c> could be used for SIMD processing on x64 systems. However <see cref="Vector{T}"/> can
/// only contain that many elements that can fit into the processor's SIMD register and thus is quite inconvenient to
/// use. So using a custom implementation.
/// </para>
/// </remarks>
[SuppressMessage(
    "Minor Code Smell",
    "S4136:Method overloads should be grouped together",
    Justification = "Helpers are moved together to a separate region")]
public class SimdCalculator
{
    private const int VectorsElementCountInt32Index = 0;
    private const int VectorElementsStartInt32Index = 1;
    private const int ResultVectorElementsStartInt32Index = 1;

    // This needs to be this low to fit all operations on the Nexys A7 board's FPGA and for the design to remain stable.
    // While only 69% of the FPGA's resources are used unfortunately we can't go above that. On the same board just
    // transforming AddVectors or SubtractVectors could fit with a degree of parallelism of more than 500. On Catapult
    // 170 will fit.
    public const int MaxDegreeOfParallelism = 20;

    public virtual void AddVectors(SimpleMemory memory) => RunSimdOperation(memory, SimdOperation.Add);

    public virtual void SubtractVectors(SimpleMemory memory) => RunSimdOperation(memory, SimdOperation.Subtract);

    public virtual void MultiplyVectors(SimpleMemory memory) => RunSimdOperation(memory, SimdOperation.Multiply);

    public virtual void DivideVectors(SimpleMemory memory) => RunSimdOperation(memory, SimdOperation.Divide);

    private void RunSimdOperation(SimpleMemory memory, SimdOperation operation)
    {
        var elementCount = memory.ReadInt32(VectorsElementCountInt32Index);

        int i = 0;

        while (i < elementCount)
        {
            var vector1 = new int[MaxDegreeOfParallelism];
            var vector2 = new int[MaxDegreeOfParallelism];

            // It is necessary so the IArraySizeHolder knows the allocation size of the variable.
#pragma warning disable S1854 // Unused assignments should be removed
            var resultVector = new int[MaxDegreeOfParallelism];
#pragma warning restore S1854 // Unused assignments should be removed

            for (int m = 0; m < MaxDegreeOfParallelism; m++)
            {
                vector1[m] = memory.ReadInt32(VectorElementsStartInt32Index + i + m);
            }

            for (int m = 0; m < MaxDegreeOfParallelism; m++)
            {
                vector2[m] = memory.ReadInt32(VectorElementsStartInt32Index + i + m + elementCount);
            }

            // This prevents the code from turning into a switch expression during decompilation. Those aren't supported
            // yet.
#pragma warning disable IDE0010 // Add missing cases
#pragma warning disable S131 // "switch/Select" statements should contain a "default/Case Else" clauses
            switch (operation)
            {
                case SimdOperation.Add:
                    resultVector = SimdOperations.AddVectors(vector1, vector2, MaxDegreeOfParallelism);
                    break;
                case SimdOperation.Subtract:
                    resultVector = SimdOperations.SubtractVectors(vector1, vector2, MaxDegreeOfParallelism);
                    break;
                case SimdOperation.Multiply:
                    resultVector = SimdOperations.MultiplyVectors(vector1, vector2, MaxDegreeOfParallelism);
                    break;
                case SimdOperation.Divide:
                    resultVector = SimdOperations.DivideVectors(vector1, vector2, MaxDegreeOfParallelism);
                    break;
            }
#pragma warning restore S131 // "switch/Select" statements should contain a "default/Case Else" clauses
#pragma warning restore IDE0010 // Add missing cases

            for (int m = 0; m < MaxDegreeOfParallelism; m++)
            {
                memory.WriteInt32(ResultVectorElementsStartInt32Index + i + m, resultVector[m]);
            }

            i += MaxDegreeOfParallelism;
        }
    }

    // Below are the methods that make the SimpleMemory-using methods easier to consume from the outside. These won't be
    // transformed into hardware since they're automatically omitted by Hastlayer (because they're not hardware entry
    // point members, nor are they used by any other transformed member). Thus you can do anything in them that is not
    // Hastlayer-compatible.

    #region Helpers

    public int[] AddVectors(int[] vector1, int[] vector2, IHastlayer hastlayer = null, IHardwareGenerationConfiguration configuration = null) =>
        RunSimdOperation(vector1, vector2, memory => AddVectors(memory), hastlayer, configuration);

    public int[] SubtractVectors(
        int[] vector1,
        int[] vector2,
        IHastlayer hastlayer = null,
        IHardwareGenerationConfiguration configuration = null) =>
        RunSimdOperation(vector1, vector2, memory => SubtractVectors(memory), hastlayer, configuration);

    public int[] MultiplyVectors(
        int[] vector1,
        int[] vector2,
        IHastlayer hastlayer = null,
        IHardwareGenerationConfiguration configuration = null) =>
        RunSimdOperation(vector1, vector2, memory => MultiplyVectors(memory), hastlayer, configuration);

    public int[] DivideVectors(
        int[] vector1,
        int[] vector2,
        IHastlayer hastlayer = null,
        IHardwareGenerationConfiguration configuration = null) =>
        RunSimdOperation(vector1, vector2, memory => DivideVectors(memory), hastlayer, configuration);

    private int[] RunSimdOperation(
        int[] vector1,
        int[] vector2,
        Action<SimpleMemory> operation,
        IHastlayer hastlayer = null,
        IHardwareGenerationConfiguration configuration = null)
    {
        SimdOperations.ThrowIfVectorsNotEquallyLong(vector1, vector2);

        var originalElementCount = vector1.Length;

        vector1 = vector1.PadToMultipleOf(MaxDegreeOfParallelism);
        vector2 = vector2.PadToMultipleOf(MaxDegreeOfParallelism);

        var elementCount = vector1.Length;
        var cellCount = 1 + (elementCount * 2);
        var memory = hastlayer is null
            ? SimpleMemory.CreateSoftwareMemory(cellCount)
            : hastlayer.CreateMemory(configuration, cellCount);

        memory.WriteInt32(VectorsElementCountInt32Index, elementCount);

        for (int i = 0; i < elementCount; i++)
        {
            memory.WriteInt32(VectorElementsStartInt32Index + i, vector1[i]);
            memory.WriteInt32(VectorElementsStartInt32Index + elementCount + i, vector2[i]);
        }

        operation(memory);

        var result = new int[elementCount];

        for (int i = 0; i < elementCount; i++)
        {
            result[i] = memory.ReadInt32(ResultVectorElementsStartInt32Index + i);
        }

        return result.CutToLength(originalElementCount);
    }

    #endregion Helpers
}
