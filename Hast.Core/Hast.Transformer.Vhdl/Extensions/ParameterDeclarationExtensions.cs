using System;

namespace ICSharpCode.Decompiler.CSharp.Syntax;

public static class ParameterDeclarationExtensions
{
    /// <summary>
    /// Determines whether the parameter has an "out-flowing" characteristic, i.e. changes to it inside the parent
    /// method should be reflected in the argument passed in too. A parameter is out-flowing if it contains a reference
    /// type or is explicitly passed by reference, or if it's an out parameter.
    /// </summary>
    public static bool IsOutFlowing(this ParameterDeclaration parameter) =>
        // If the parameter is a value type then still it needs to be out-flowing if this is a constructor.
        parameter.GetActualType().IsReferenceType == true ||
        (parameter.FindFirstParentEntityDeclaration().GetFullName().IsConstructorName() &&
            parameter.FindFirstParentTypeDeclaration().GetFullName() == parameter.GetActualTypeFullName()) ||
        parameter.ParameterModifier == ParameterModifier.Out ||
        parameter.ParameterModifier == ParameterModifier.Ref;
}
