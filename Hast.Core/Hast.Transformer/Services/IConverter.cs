using Hast.Common.Interfaces;
using Hast.Layer;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Services;

/// <summary>
/// A common interface for any converters utilized by <see cref="ITransformer"/>.
/// </summary>
public interface IConverter : IDependency
{
    /// <summary>
    /// Gets the name of the converter. Normally this is the type name and in this case it should be left as the
    /// interface implementation.
    /// </summary>
    string Name => GetType().Name;

    /// <summary>
    /// Gets the type names of dependency services used for topological sorting.
    /// </summary>
    IEnumerable<string> Dependencies => Enumerable.Empty<string>();

    /// <summary>
    /// Performs a conversion operation by altering the <paramref name="syntaxTree"/>.
    /// </summary>
    void Convert(SyntaxTree syntaxTree, IHardwareGenerationConfiguration configuration, IKnownTypeLookupTable knownTypeLookupTable);
}
