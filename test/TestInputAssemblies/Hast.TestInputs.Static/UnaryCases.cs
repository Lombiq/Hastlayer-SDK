namespace Hast.TestInputs.Static;

public class UnaryCases
{
    public void IncrementDecrement(int input)
    {
        var array = new int[5];
        if (input < 10)
        {
            // These unary expressions will remain in the AST and thus need to be handled.
            array[input++] = 3;
            array[input--] = 3;
        }
    }
}
