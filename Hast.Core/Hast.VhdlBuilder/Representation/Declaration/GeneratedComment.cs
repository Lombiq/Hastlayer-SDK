using System;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

/// <summary>
/// A VHDL comment that produces its value when VHDL code is generated.
/// </summary>
[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class GeneratedComment : IVhdlElement
{
    private readonly Func<IVhdlGenerationOptions, string> _generator;

    public GeneratedComment(Func<IVhdlGenerationOptions, string> generator) => _generator = generator;

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        new LineComment(_generator(vhdlGenerationOptions)).ToVhdl(vhdlGenerationOptions);
}
