using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.Decompiler.CSharp.Syntax;

public static class SimpleMemoryMethodDeclarationExtensions
{
    public static string GetSimpleMemoryParameterName(this MethodDeclaration method)
    {
        var parameter = method.Parameters.SingleOrDefault(p => p.IsSimpleMemoryParameter());
        if (parameter == null) return null;
        return parameter.Name;
    }

    public static IEnumerable<ParameterDeclaration> GetNonSimpleMemoryParameters(this MethodDeclaration method) =>
        method.Parameters.Where(p => !p.IsSimpleMemoryParameter());
}
