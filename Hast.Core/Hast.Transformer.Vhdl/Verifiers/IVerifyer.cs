using Hast.Common.Interfaces;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Vhdl.Verifiers;

/// <summary>
/// A service that verifies the code and throws an exception if anything is wrong.
/// </summary>
public interface IVerifyer : IDependency
{
    /// <summary>
    /// Verifies the code in <paramref name="syntaxTree"/> and throws an exception if anything is wrong.
    /// </summary>
    void Verify(SyntaxTree syntaxTree, ITransformationContext transformationContext);
}
