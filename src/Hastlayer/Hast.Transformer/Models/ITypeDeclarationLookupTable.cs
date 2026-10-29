using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Models;

/// <summary>
/// Retrieves the type declaration, given the type's full name.
/// </summary>
public interface ITypeDeclarationLookupTable
{
    /// <summary>
    /// Retrieves the type declaration, given the type's full name.
    /// </summary>
    /// <param name="fullName">
    /// The type's full name (including the namespace) to look up the type declaration for.
    /// </param>
    /// <returns>The retrieved <see cref="TypeDeclaration"/> if found or <see langword="null"/> otherwise.</returns>
    TypeDeclaration Lookup(string fullName);
}

public static class TypeDeclarationLookupTableExtensions
{
    /// <summary>
    /// Retrieves the type declaration, given an AST type.
    /// </summary>
    /// <param name="type">The AST type to look up the type declaration for.</param>
    /// <returns>The retrieved <see cref="TypeDeclaration"/> if found or <see langword="null"/> otherwise.</returns>
    public static TypeDeclaration Lookup(this ITypeDeclarationLookupTable typeDeclarationLookupTable, AstType type) =>
        typeDeclarationLookupTable.Lookup(type.GetActualTypeFullName());

    public static TypeDeclaration Lookup(
        this ITypeDeclarationLookupTable typeDeclarationLookupTable,
        TypeReferenceExpression typeReferenceExpression) =>
        typeDeclarationLookupTable.Lookup(typeReferenceExpression.Type);
}
