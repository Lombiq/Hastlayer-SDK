using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Syntax;

public static class SimpleMemoryParameterDeclarationExtensions
{
    public static bool IsSimpleMemoryParameter(this ParameterDeclaration parameterDeclaration) =>
        parameterDeclaration.Type.GetActualType().IsSimpleMemory();
}
