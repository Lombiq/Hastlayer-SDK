using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.Models;

/// <summary>
/// The result that member transformers return.
/// </summary>
/// <remarks>
/// <para>
/// Declarations and Body wouldn't be needed, since they can be generated from the state machine. However by requiring
/// transformers to build them the process can be parallelized better.
/// </para>
/// </remarks>
public interface IMemberTransformerResult
{
    /// <summary>
    /// Gets the member that was transformed.
    /// </summary>
    EntityDeclaration Member { get; }

    /// <summary>
    /// Gets a value indicating whether this member is an entry point or one invoked directly or indirectly by the entry
    /// point.
    /// </summary>
    bool IsHardwareEntryPointMember { get; }

    /// <summary>
    /// Gets the returned results as <see cref="IArchitectureComponentResult"/>s.
    /// </summary>
    IEnumerable<IArchitectureComponentResult> ArchitectureComponentResults { get; }
}
