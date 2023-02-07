namespace Hast.TestInputs.Static;

public class LoopCases
{
    public void BreakInLoop(int input)
    {
        var sum = input;

        for (int i = 0; i < input; i++)
        {
            sum += i;

            if (sum > 10)
            {
#pragma warning disable S1227 // break statements should not be used except for switch cases
                break;
#pragma warning restore S1227 // break statements should not be used except for switch cases
            }
        }
    }

    public void BreakInLoopInLoop(int input)
    {
        var sum = input;

        for (int i = 0; i < input; i++)
        {
            for (int x = 0; x < i; x++)
            {
                sum += i;

                if (sum > 10)
                {
#pragma warning disable S1227 // break statements should not be used except for switch cases
                    break;
#pragma warning restore S1227 // break statements should not be used except for switch cases
                }
            }
        }
    }
}
