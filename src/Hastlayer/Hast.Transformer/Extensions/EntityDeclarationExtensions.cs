using Hast.Transformer.Helpers;
using System;

namespace ICSharpCode.Decompiler.CSharp.Syntax;

public static class EntityDeclarationExtensions
{
    /// <summary>
    /// Finds the method in an interface that the declaration implements a method of, if any.
    /// </summary>
    /// <returns>
    /// The <see cref="TypeDeclaration"/> of the interface's method that the declaration implements a method of, or <see
    /// langword="null"/> if the declaration is not an implementation of any method of any interface.
    /// </returns>
    public static T FindImplementedInterfaceMethod<T>(this T member, Func<AstType, TypeDeclaration> lookupDeclaration)
        where T : EntityDeclaration
    {
        var privateImplementationType = member is MethodDeclaration ?
            ((MethodDeclaration)(object)member).PrivateImplementationType :
            ((PropertyDeclaration)(object)member).PrivateImplementationType;

        // This is an explicitly implemented method so just returning the interface's type declaration directly.
        if (!privateImplementationType.IsNull)
        {
            var interfaceDeclaration = lookupDeclaration(privateImplementationType);
            return interfaceDeclaration?.FindMatchingMember(member, lookupDeclaration);
        }

        // Otherwise if it's not public it can't be a member declared in an interface.
        if (member.Modifiers != Modifiers.Public) return null;

        // Searching for an implemented interface with the same member.
        var parent = (TypeDeclaration)member.Parent;
        // BaseTypes are flattened, so interface inheritance is taken into account.
        foreach (var baseType in parent.BaseTypes)
        {
            if (baseType.NodeType != NodeType.TypeReference) continue;

            // baseType is a TypeReference but we need the corresponding TypeDeclaration to check for the methods.
            var baseTypeDeclaration = lookupDeclaration(baseType);

            if (baseTypeDeclaration == null)
            {
                ExceptionHelper.ThrowDeclarationNotFoundException(baseType.GetFullName(), member);

                // This won't even be reached, but it helps the code analyser see that the above line breaks the
                // execution path.
                return null;
            }

            if (baseTypeDeclaration.ClassType != ClassType.Interface) continue;

            var matchingMethod = baseTypeDeclaration.FindMatchingMember(member, lookupDeclaration);
            if (matchingMethod != null) return matchingMethod;
        }

        return null;
    }

    public static bool IsReadOnlyMember(this EntityDeclaration entityDeclaration) =>
        entityDeclaration.Is<PropertyDeclaration>(property => property.Setter == Accessor.Null) ||
        entityDeclaration.Is<FieldDeclaration>(field => field.HasModifier(Modifiers.Readonly));
}
