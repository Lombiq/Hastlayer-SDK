using System;

namespace ICSharpCode.Decompiler.CSharp.Syntax;

public static class AstTypeExtensions
{
    public static bool AstTypeEquals(this AstType astType, AstType other, Func<AstType, TypeDeclaration> lookupDeclaration)
    {
        if (astType is PrimitiveType && other is PrimitiveType)
        {
            return ((PrimitiveType)astType).Keyword == ((PrimitiveType)other).Keyword;
        }

        if (astType is ComposedType && other is ComposedType)
        {
            return ((ComposedType)astType).BaseType.AstTypeEquals(((ComposedType)other).BaseType, lookupDeclaration);
        }

        return lookupDeclaration(astType) == lookupDeclaration(other);
    }

    public static bool IsArray(this AstType type) =>
        type is ComposedType composedType && composedType.ArraySpecifiers.Count != 0;
}
