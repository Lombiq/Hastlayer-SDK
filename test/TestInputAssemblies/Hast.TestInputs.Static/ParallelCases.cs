using System.Threading.Tasks;

namespace Hast.TestInputs.Static;

public class ParallelCases
{
    public void WhenAllWhenAnyAwaitedTasks(uint input)
    {
        var tasks = new Task<bool>[3];

        for (uint i = 0; i < 3; i++)
        {
            tasks[i] = Task.Factory.StartNew(
                indexObject =>
                {
                    var index = (uint)indexObject;
                    index += input;
                    return index % 2 == 0;
                },
                i);
        }

        // These two after each other don't make sense, but the test result will be still usable just for static
        // checking.
        Task.WhenAll(tasks).Wait();
        Task.WhenAny(tasks).Wait();
    }

    public void ObjectUsingTasks(uint input)
    {
        var tasks = new Task<bool>[3];

        for (uint i = 0; i < 3; i++)
        {
            tasks[i] = Task.Factory.StartNew(
                indexObject =>
                {
                    var index = (uint)indexObject;
                    index += input;
                    return new Calculator { Number = index }.IsEven();
                },
                i);
        }

        Task.WhenAll(tasks).Wait();
    }

    private sealed class Calculator
    {
        public uint Number { get; set; }

        public bool IsEven() => Number % 2 == 0;
    }
}
