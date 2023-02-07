using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

/// <summary>
/// A VHDL comment line.
/// </summary>
/// <seealso cref="BlockComment"/>
[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class LineComment : IVhdlElement
{
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the comment won't be omitted even if this is configured in <see
    /// cref="IVhdlGenerationOptions"/>.
    /// </summary>
    public bool CantBeOmitted { get; set; }

    public LineComment(string text) => Text = text;

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions)
    {
        // There are no block comments in VHDL prior to VHDL 2008 so if the code is not formatted there can't be any
        // comments.
        if (!vhdlGenerationOptions.FormatCode || (vhdlGenerationOptions.OmitComments && !CantBeOmitted))
        {
            return string.Empty;
        }

        return "-- " + Text + vhdlGenerationOptions.NewLineIfShouldFormat();
    }
}
