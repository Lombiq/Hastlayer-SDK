using Hast.Common.Interfaces;
using Hast.Layer;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Services;

/// <summary>
/// When a member's instance count is &gt;1 the members invoked by it should have at least that instance count. This
/// service adjusts these instance counts.
/// </summary>
public interface IInvocationInstanceCountAdjuster : IDependency
{
    /// <summary>
    /// Updates the instance count of invoked members.
    /// </summary>
    void AdjustInvocationInstanceCounts(SyntaxTree syntaxTree, IHardwareGenerationConfiguration configuration);
}
