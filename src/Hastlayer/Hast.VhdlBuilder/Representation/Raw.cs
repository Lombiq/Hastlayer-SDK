using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Hast.VhdlBuilder.Representation;

/// <summary>
/// Any VHDL code that's not implemented as a class.
/// </summary>
[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Raw : IVhdlElement
{
    public string Source { get; set; }
    public IList<IVhdlElement> Parameters { get; } = new List<IVhdlElement>();

    public Raw()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Raw"/> class. The raw VHDL source code to add to the syntax tree.
    /// Can contain lazily evaluated parameters with the <c>string.Format()</c> syntax.
    /// </summary>
    public Raw(string source, params IVhdlElement[] parameters)
    {
        Source = source;
        Parameters = parameters.ToList();
    }

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Parameters == null || !Parameters.Any()
            ? Source
            : string.Format(
                CultureInfo.InvariantCulture,
                Source,
                Parameters.Select(element => (object)element.ToVhdl(vhdlGenerationOptions)).ToArray());
}
