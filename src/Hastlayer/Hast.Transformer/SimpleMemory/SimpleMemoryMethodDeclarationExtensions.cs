using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.Decompiler.CSharp.Syntax;

public static class SimpleMemoryMethodDeclarationExtensions
{
    public static string GetSimpleMemoryParameterName(this MethodDeclaration method) =>
        method.Parameters.SingleOrDefault(parameter => parameter.IsSimpleMemoryParameter())?.Name;

    public static IEnumerable<ParameterDeclaration> GetNonSimpleMemoryParameters(this MethodDeclaration method) =>
        method.Parameters.Where(p => !p.IsSimpleMemoryParameter());
}
