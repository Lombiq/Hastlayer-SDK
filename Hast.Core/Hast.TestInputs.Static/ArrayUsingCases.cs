namespace Hast.TestInputs.Static;

public class ArrayUsingCases
{
    public void PassArrayToConstructor()
    {
        var array = new int[5];
        var arrayHolder = new ArrayHolder(array);
        var arrayLength = arrayHolder.Array.Length;
    }

    public void PassArrayFromMethod()
    {
        var array = ArrayProducingMethod(5);
    }

    private int[] ArrayProducingMethod(int arrayLength)
    {
        var array = new int[arrayLength];
        array[3] = 10;
        return array;
    }

    private sealed class ArrayHolder
    {
        public int[] Array { get; }

        public ArrayHolder(int[] array) => Array = array;
    }
}
