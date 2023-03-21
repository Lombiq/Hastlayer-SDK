using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Hast.Common.Extensions;

// Not in the same namespace as string so it only appears when you need it.
public static class MemberNameExtensions
{
    /// <summary>
    /// Creates an alternate versions of a member name if the full member name contains both a class and an interface
    /// reference (as it is with explicitly implemented members).
    /// </summary>
    /// <remarks>
    /// <para>
    /// E.g. a member name as stored in the hardware description can be: "System.Int32
    /// Hast.Tests.TestAssembly1.ComplexTypes.ComplexTypeHierarchy::Hast.Tests.TestAssembly1.
    /// ComplexTypes.IInterface1.Interface1Method1(System.Int32)" We create two alternates from this:
    /// 1) "System.Int32 Hast.Tests.TestAssembly1.ComplexTypes.ComplexTypeHierarchy::Interface1Method1(System.Int32)"
    /// 2) "System.Int32 Hast.Tests.TestAssembly1.ComplexTypes.IInterface1::Interface1Method1(System.Int32)".
    /// </para>
    /// </remarks>
    /// <returns>Alternate member names, if any.</returns>
    public static IEnumerable<string> GetMemberNameAlternates(this string memberFullName)
    {
        var sides = memberFullName.Split(new[] { "::" }, StringSplitOptions.RemoveEmptyEntries);

        // If there are no dots before the member name that means this full name doesn't contain an interface reference.
        if (sides.Length != 2 ||
            !sides[1].Contains('.') ||
            sides[1].IndexOfOrdinal(".") > sides[1].IndexOfOrdinal("("))
        {
            return Enumerable.Empty<string>();
        }

        var methodName = memberFullName.RegexMatch(@"\.([a-z0-9]*)\(", RegexOptions.Compiled | RegexOptions.IgnoreCase).Groups[1];
        var returnType = sides[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];

        return new[]
        {
            // 1. alternate:
            sides[0] + "::" + sides[1][sides[1].IndexOfOrdinal(methodName + "(")..],
            // 2. alternate:
            returnType + " " + sides[1].Replace($".{methodName}(", $"::{methodName}("),
        };
    }
}
