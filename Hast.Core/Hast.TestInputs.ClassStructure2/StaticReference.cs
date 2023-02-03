using Hast.TestInputs.ClassStructure1.ComplexTypes;

namespace Hast.TestInputs.ClassStructure2;

/// <summary>
/// Demonstrates access to a static class (similar how e.g. the Math class would be used).
/// </summary>
public class StaticReference
{
    public virtual void StaticClassUsingMethod()
    {
        StaticClass.StaticMethod();
        if (true)
        {
            var x = 1;
            var y = x;
        }

        var x2 = 2;
        var y2 = x2;
    }
}
