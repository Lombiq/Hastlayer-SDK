namespace Hast.TestInputs.ClassStructure1.ComplexTypes;

/// <summary>
/// Demonstrates a static class in a separate assembly that should be usable from transformed methods.
/// </summary>
public static class StaticClass
{
    public static void StaticMethod()
    {
        var x = 1;
    }
}
