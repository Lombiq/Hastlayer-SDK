namespace Hast.TestInputs.ClassStructure1.ComplexTypes;

/// <summary>
/// Descendant for testing interface inheritance.
/// </summary>
public interface IInterface1 : IBaseInterface
{
    /// <summary>
    /// Entry point.
    /// </summary>
    void Interface1Method1();

    /// <summary>
    /// Unused method.
    /// </summary>
    void Interface1Method2(bool isTrue);
}
