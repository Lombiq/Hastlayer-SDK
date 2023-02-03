using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Services;

// This should perhaps rather be in Hast.Communication because it depends on the Communication component and its proxy
// generator on what can be used as hardware entry point members. See / <see
// href="https://github.com/Lombiq/Hastlayer-SDK/issues/99"/>.
public class MemberSuitabilityChecker : IMemberSuitabilityChecker
{
    public bool IsSuitableHardwareEntryPointMember(EntityDeclaration member, ITypeDeclarationLookupTable typeDeclarationLookupTable)
    {
        if (member is MethodDeclaration method &&
            method.Parent is TypeDeclaration declaration &&
            // If it's a public virtual method,
            (method.Modifiers == (Modifiers.Public | Modifiers.Virtual) ||
            // or a public override method which is the same in F# (no direct virtual there, just abstract and override)
            method.Modifiers == (Modifiers.Public | Modifiers.Override) ||
            // or a public virtual async method,
            method.Modifiers == (Modifiers.Public | Modifiers.Virtual | Modifiers.Async) ||
            // or a public method that implements an interface.
            method.FindImplementedInterfaceMethod(typeDeclarationLookupTable.Lookup) != null))
        {
            var parent = declaration;
            return parent.ClassType == ClassType.Class && parent.Modifiers == Modifiers.Public;
        }

        return false;
    }
}
