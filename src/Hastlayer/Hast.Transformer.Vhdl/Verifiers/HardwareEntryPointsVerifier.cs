using Hast.Transformer.Models;
using Hast.Transformer.Services;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Linq;

namespace Hast.Transformer.Vhdl.Verifiers;

/// <summary>
/// Checks if hardware entry point types are suitable for transforming.
/// </summary>
public class HardwareEntryPointsVerifier : IVerifyer
{
    private readonly IMemberSuitabilityChecker _memberSuitabilityChecker;

    public HardwareEntryPointsVerifier(IMemberSuitabilityChecker memberSuitabilityChecker) =>
        _memberSuitabilityChecker = memberSuitabilityChecker;

    public void Verify(SyntaxTree syntaxTree, ITransformationContext transformationContext) =>
        VerifyHardwareEntryPoints(syntaxTree, transformationContext.TypeDeclarationLookupTable);

    /// <summary>
    /// Checks if hardware entry point types are suitable for transforming.
    /// </summary>
    private void VerifyHardwareEntryPoints(SyntaxTree syntaxTree, ITypeDeclarationLookupTable typeDeclarationLookupTable)
    {
        var hardwareEntryPointTypes = syntaxTree
            .GetTypes()
            .Where(entity =>
                entity.Is<TypeDeclaration>(type =>
                    type.Members.Any(member =>
                        _memberSuitabilityChecker.IsSuitableHardwareEntryPointMember(member, typeDeclarationLookupTable))))
            .Cast<TypeDeclaration>();

        foreach (var type in hardwareEntryPointTypes)
        {
            var unsupportedMembers = type
                .Members
                .Where(member =>
                    (member is FieldDeclaration or PropertyDeclaration && !member.HasModifier(Modifiers.Const)) ||
                    member.GetFullName().IsConstructorName());
            if (unsupportedMembers.Any())
            {
                throw new NotSupportedException(
                    "Fields, properties and constructors are not supported in hardware entry point types. The type " +
                    type.GetFullName() + " contains the following unsupported members: " +
                    string.Join(", ", unsupportedMembers.Select(member => member.GetFullName())));
            }
        }
    }
}
