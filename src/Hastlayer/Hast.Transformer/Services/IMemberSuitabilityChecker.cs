using Hast.Common.Interfaces;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Services;

/// <summary>
/// Checks whether a member is suitable to be among the hardware members that are directly executable from the host
/// computer.
/// </summary>
public interface IMemberSuitabilityChecker : IDependency
{
    /// <summary>
    /// Checks whether a member is suitable to be among the hardware members that are directly executable from the host
    /// computer.
    /// </summary>
    bool IsSuitableHardwareEntryPointMember(EntityDeclaration member, ITypeDeclarationLookupTable typeDeclarationLookupTable);
}
