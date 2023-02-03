namespace Hast.VhdlBuilder.Representation.Declaration;

/// <summary>
/// Represents a VHDL element that is a declaration which can be referenced from other places. E.g. a variable
/// declaration can be referenced in a variable assignment.
/// </summary>
public interface IReferenceableDeclaration : IVhdlElement
{
}

/// <summary>
/// Represents a VHDL element that is <see cref="IReferenceableDeclaration"/> and also can produce a VHDL reference to
/// itself. E.g. an implementation of this can be a signal or variable declaration that can produce a reference to
/// itself to be used in the body of a process.
/// </summary>
/// <typeparam name="T">The type of the reference.</typeparam>
public interface IReferenceableDeclaration<out T> : IReferenceableDeclaration
    where T : IVhdlElement
{
    /// <summary>
    /// Creates a reference to the declared type to be used in the body of entities.
    /// </summary>
    T ToReference();
}
