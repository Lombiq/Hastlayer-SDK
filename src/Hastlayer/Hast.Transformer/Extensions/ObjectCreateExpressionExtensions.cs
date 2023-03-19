using Hast.Transformer.Helpers;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using System.Linq;

namespace ICSharpCode.Decompiler.CSharp.Syntax;

public static class ObjectCreateExpressionExtensions
{
    public static string GetConstructorFullName(this ObjectCreateExpression objectCreateExpression) =>
        objectCreateExpression.GetResolveResult<InvocationResolveResult>()?.Member.GetFullName();

    public static EntityDeclaration FindConstructorDeclaration(
        this ObjectCreateExpression objectCreateExpression,
        ITypeDeclarationLookupTable typeDeclarationLookupTable)
    {
        var constructorName = objectCreateExpression.GetConstructorFullName();

        if (string.IsNullOrEmpty(constructorName)) return null;

        var createdTypeName = objectCreateExpression.Type.GetFullName();

        var constructorType = typeDeclarationLookupTable.Lookup(createdTypeName);

        if (constructorType == null) ExceptionHelper.ThrowDeclarationNotFoundException(createdTypeName, objectCreateExpression);

        return constructorType!
            .Members
            .SingleOrDefault(member => member.GetFullName() == constructorName);
    }
}
