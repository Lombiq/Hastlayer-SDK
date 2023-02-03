namespace Hast.TestInputs.ClassStructure1.ComplexTypes;

/// <summary>
/// Demonstrates a static class in a separate assembly that should be usable from transformed methods.
/// </summary>
public static class StaticClass
{
    public static void StaticMethod()
    {
        // Intentionally blank, anything here would be optimized out by the compiler anyway, this being a blank and pure
        // method.
    }
}
