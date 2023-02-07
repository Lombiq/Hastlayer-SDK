using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;

namespace Hast.Transformer.Helpers;

public static class TypeHelper
{
    public static AstType CreateAstType(IType type)
    {
        // CSharpDecompiler.CreateAstBuilder() constructs a TypeSystemAstBuilder object like this.
        var typeSystemAstBuilder = new TypeSystemAstBuilder
        {
            ShowAttributes = true,
            AlwaysUseShortTypeNames = true,
            AddResolveResultAnnotations = true,
        };

        return typeSystemAstBuilder.ConvertType(type);
    }
}
