using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp.Syntax;

public static class MethodDeclarationExtensions
{
    public static bool IsConstructor(this MethodDeclaration methodDeclaration) =>
        (methodDeclaration.GetMemberResolveResult()?.Member as IMethod)?.IsConstructor == true;
}
