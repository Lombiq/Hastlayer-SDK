using System.Linq;

namespace ICSharpCode.Decompiler.CSharp.Syntax;

public static class AstTypeExtensions
{
    public static AstType GetStoredTypeOfTaskResultArray(this AstType type) => ((SimpleType)type).TypeArguments.Single();
}
