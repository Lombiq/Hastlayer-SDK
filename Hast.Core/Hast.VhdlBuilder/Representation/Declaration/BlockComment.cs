using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Hast.VhdlBuilder.Representation.Declaration;

/// <summary>
/// A VHDL block comment, consisting of multiple comment lines.
/// </summary>
/// <seealso cref="LineComment"/>
[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class BlockComment : IVhdlElement
{
    public IList<string> Lines { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the comment won't be omitted even if this is configured in <see
    /// cref="IVhdlGenerationOptions"/>.
    /// </summary>
    public bool CantBeOmitted { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockComment"/> class. Initialized with a string that contains a
    /// newline character-delimited block of text, corresponding to lines of the block comment.
    /// </summary>
    public BlockComment(string textBlock)
        : this(textBlock.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
    {
    }

    public BlockComment(params string[] lines)
        : this() => Lines = lines.ToList();

    public BlockComment() => Lines = new List<string>();

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions)
    {
        if (vhdlGenerationOptions.OmitComments && !CantBeOmitted) return string.Empty;

        var stringBuilder = new StringBuilder(Lines.Count);

        foreach (var line in Lines)
        {
            stringBuilder.Append(new LineComment(line) { CantBeOmitted = true }.ToVhdl(vhdlGenerationOptions));
        }

        return stringBuilder.ToString();
    }
}
