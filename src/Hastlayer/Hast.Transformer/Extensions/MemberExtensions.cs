using System.Linq;

namespace ICSharpCode.Decompiler.TypeSystem;

public static class MemberExtensions
{
    public static string GetFullName(this IMember member)
    {
        var name = member.ReturnType.GetFullName() + " ";

        name += $"{member.DeclaringType.GetFullName()}::{member.Name}";

        if (member is IMethod method)
        {
            // There shouldn't be a space after commas so it follows Cecil naming conventions.
            name += $"({string.Join(',', method.Parameters.Select(parameter => parameter.Type.GetFullName()))})";
        }

        return name;
    }
}
