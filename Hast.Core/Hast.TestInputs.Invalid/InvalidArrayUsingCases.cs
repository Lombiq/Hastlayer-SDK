using System;
using System.Collections.Immutable;

namespace Hast.TestInputs.Invalid;

public class InvalidArrayUsingCases
{
    public void InvalidArrayAssignment()
    {
        // Since array size can only be statically defined using the same method (which has only one hardware array
        // "instance") invocations with different array sizes are invalid.

        var array1 = new[] { 1 };
        var value1 = GetItemValuePlusOne(array1, 0);
        var array2 = new[] { 1, 2 };
        var value2 = GetItemValuePlusOne(array2, 0);
    }

    public void ArraySizeIsNotStatic(int arraySize)
    {
        var array = new int[arraySize + 1];
    }

    public void ArrayCopyToIsNotStaticCopy(int input)
    {
        var array1 = new int[5];
        var array2 = new int[5];
        Array.Copy(array1, array2, input);
    }

    public void UnsupportedImmutableArrayCreateRangeUsage()
    {
        var immutableArray1 = ImmutableArray.CreateRange(new[] { 1 });
        var immutableArray2 = ImmutableArray.CreateRange(immutableArray1, item => item);
    }

    public void MultiDimensionalArray()
    {
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
        var array = new int[2, 3];
#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional
        array[0, 1] = 10;
    }

    public void NullAssignment()
    {
#pragma warning disable S1854 // Unused assignments should be removed
        var array1 = new int[5];
#pragma warning restore S1854 // Unused assignments should be removed
        array1 = null;
#pragma warning disable S2259 // Null pointers should not be dereferenced
        var x = array1.Length; // Intentional, we are testing null assignment.
#pragma warning restore S2259 // Null pointers should not be dereferenced
        var y = -x;
    }

    private int GetItemValuePlusOne(int[] array, int itemIndex) => array[itemIndex] + 1;
}
