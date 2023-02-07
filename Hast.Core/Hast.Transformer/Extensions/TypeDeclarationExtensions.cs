using System;
using System.Linq;

namespace ICSharpCode.Decompiler.CSharp.Syntax;

public static class TypeDeclarationExtensions
{
    /// <summary>
    /// Searches for a member on the type that has the same signature as the supplied member.
    /// </summary>
    /// <returns>The declaration of the matching member if found, <see langword="null"/> otherwise.</returns>
    public static T FindMatchingMember<T>(
        this TypeDeclaration typeDeclaration,
        T memberDeclaration,
        Func<AstType, TypeDeclaration> lookupDeclaration)
        where T : EntityDeclaration
    {
        var isMethod = memberDeclaration is MethodDeclaration;

        // Searching for members that have the exact same signature.
        return (T)typeDeclaration.Members.SingleOrDefault(member =>
        {
            if (member.Name != memberDeclaration.Name) return false;

            var isMatching =
                // Only checking for modifiers if the type is not an interface.
                (typeDeclaration.ClassType == ClassType.Interface || member.Modifiers == memberDeclaration.Modifiers) &&
                member.ReturnType.AstTypeEquals(memberDeclaration.ReturnType, lookupDeclaration);

            if (!isMatching || !isMethod || member is not MethodDeclaration) return isMatching;

            var methodToMatch = (MethodDeclaration)(object)memberDeclaration;
            var currentMethod = (MethodDeclaration)member;

            if (currentMethod.Parameters.Count != methodToMatch.Parameters.Count) return true;

            foreach (var interfaceMethodParameter in currentMethod.Parameters)
            {
                if (!methodToMatch.Parameters.Any(
                    parameter => parameter.Type.AstTypeEquals(interfaceMethodParameter.Type, lookupDeclaration)))
                {
                    return false;
                }
            }

            return true;
        });
    }
}
