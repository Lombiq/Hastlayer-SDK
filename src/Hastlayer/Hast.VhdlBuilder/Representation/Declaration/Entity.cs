using Hast.VhdlBuilder.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Entity : INamedElement, IDeclarableElement
{
    private const string SafeNameCharacterSet = "a-z0-9_";

    private string _name;

    /// <summary>
    /// Gets or sets the name of the VHDL Entity. Keep in mind that Entity names can't be extended identifiers thus they
    /// can only contain alphanumerical characters.
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (!value.RegexIsMatch("^[" + SafeNameCharacterSet + "]*$", RegexOptions.IgnoreCase))
            {
                throw new ArgumentException("VHDL Entity names can only contain alphanumerical characters.");
            }

            _name = value;
        }
    }

    public IList<Generic> Generics { get; } = new List<Generic>();
    public IList<Port> Ports { get; } = new List<Port>();
    public IList<IVhdlElement> Declarations { get; } = new List<IVhdlElement>();

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions)
    {
        var name = vhdlGenerationOptions.ShortenName(Name);
        return Terminated.Terminate(
            "entity " + name + " is " + vhdlGenerationOptions.NewLineIfShouldFormat() +
                ((Generics != null && Generics.Any() ?
                    Generics.ToVhdl(vhdlGenerationOptions).IndentLinesIfShouldFormat(vhdlGenerationOptions) :
                    string.Empty) +

                "port(" + vhdlGenerationOptions.NewLineIfShouldFormat() +
                    (Ports
                        // The last port shouldn't be terminated by a semicolon...
                        .ToVhdl(vhdlGenerationOptions, Terminated.Terminator(vhdlGenerationOptions), string.Empty) +
                        vhdlGenerationOptions.NewLineIfShouldFormat())
                    .IndentLinesIfShouldFormat(vhdlGenerationOptions) +
                Terminated.Terminate(")", vhdlGenerationOptions) +

                Declarations.ToVhdl(vhdlGenerationOptions))
                .IndentLinesIfShouldFormat(vhdlGenerationOptions) +
            "end " + name,
            vhdlGenerationOptions);
    }

    /// <summary>
    /// Converts a string to be a safe Entity name, i.e. strips and substitutes everything not suited.
    /// </summary>
    /// <param name="name">The unsafe name to convert.</param>
    /// <returns>The cleaned name.</returns>
    public static string ToSafeEntityName(string name) =>
        name.RegexReplace("[^" + SafeNameCharacterSet + "]", "_", RegexOptions.IgnoreCase);
}
