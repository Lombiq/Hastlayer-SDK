using Hast.Layer;
using Hast.Transformer.Vhdl.ArchitectureComponents;
using Hast.VhdlBuilder.Representation;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.Models;

/// <summary>
/// Represents the output of a VHDL component (see <see cref="IArchitectureComponent"/>).
/// </summary>
public interface IArchitectureComponentResult
{
    /// <summary>
    /// Gets the component this is the result of.
    /// </summary>
    IArchitectureComponent ArchitectureComponent { get; }

    /// <summary>
    /// Gets the declarations from <see cref="IArchitectureComponent.BuildDeclarations"/>.
    /// </summary>
    IVhdlElement Declarations { get; }

    /// <summary>
    /// Gets the body from <see cref="IArchitectureComponent.BuildBody"/>.
    /// </summary>
    IVhdlElement Body { get; }

    /// <summary>
    /// Gets the collection of warnings generated within this component's scope.
    /// </summary>
    IEnumerable<ITransformationWarning> Warnings { get; }
}
