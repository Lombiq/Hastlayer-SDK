using System.Threading.Tasks;

namespace Hast.TestInputs.Invalid;

public class InvalidParallelCases
{
    public void InvalidExternalVariableAssignment(uint input)
    {
        var task = Task.Factory.StartNew(
            () =>
            {
                // If this would be something like input = 5 then it would correctly be substituted with a const.
                input += 5;
                return input == 5;
            },
            default,
            TaskCreationOptions.None,
            TaskScheduler.Default);
    }
}
