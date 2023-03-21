using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.Semantics;

public static class MemberResolveResultExtensions
{
    public static string GetFullName(this MemberResolveResult memberResolveResult) =>
        memberResolveResult.Member.GetFullName();
}
