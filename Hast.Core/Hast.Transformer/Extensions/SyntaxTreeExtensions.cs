using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.Decompiler.CSharp.Syntax;

public static class SyntaxTreeExtensions
{
    public static IEnumerable<TypeDeclaration> GetAllTypeDeclarations(this SyntaxTree syntaxTree) =>
        syntaxTree.GetTypes(includeInnerTypes: true).Where(type => type is TypeDeclaration).Cast<TypeDeclaration>();
}
