using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;
using System;
using System.Linq;

namespace Hast.VhdlBuilder.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Converts a string identifier to be used in VHDL as an extended identifier.
    /// </summary>
    public static string ToExtendedVhdlId(this string id)
    {
        if (id.StartsWithOrdinal(@"\") && id.EndsWithOrdinal(@"\")) return id;
        return @"\" + id + @"\";
    }

    /// <summary>
    /// Trims the VHDL extended identifier delimiters (backslash characters) from the given string.
    /// </summary>
    public static string TrimExtendedVhdlIdDelimiters(this string id) => id.Trim('\\');

    /// <summary>
    /// Converts a string identifier to a VHDL identifier value object as an extended VHDL identifier.
    /// </summary>
    public static Value ToExtendedVhdlIdValue(this string id) => id.ToExtendedVhdlId().ToVhdlIdValue();

    /// <summary>
    /// Converts a string identifier to a VHDL identifier value object.
    /// </summary>
    public static Value ToVhdlIdValue(this string id) => new IdentifierValue(id);

    public static Value ToVhdlValue(this string valueString, DataType dataType) =>
        new()
        { Content = valueString, DataType = dataType };

    /// <summary>
    /// Converts a variable name to a VHDL variable reference.
    /// </summary>
    public static DataObjectReference ToVhdlVariableReference(this string variableName) =>
        new()
        {
            DataObjectKind = DataObjectKind.Variable,
            Name = variableName,
        };

    /// <summary>
    /// Converts a signal name to a VHDL signal reference.
    /// </summary>
    public static DataObjectReference ToVhdlSignalReference(this string signalName) =>
        new()
        {
            DataObjectKind = DataObjectKind.Signal,
            Name = signalName,
        };

    public static string IndentLinesIfShouldFormat(this string vhdl, IVhdlGenerationOptions vhdlGenerationOptions) =>
        // Empty new lines won't be indented as they can contain different blocks. A space will be added if no
        // formatting is uses so the code remains syntactically correct even if being just one line.
        string.Join(vhdlGenerationOptions.NewLineIfShouldFormat(), vhdl
            .Split(new[] { Environment.NewLine }, StringSplitOptions.None)
            .Select(line => (!string.IsNullOrEmpty(line) ? vhdlGenerationOptions.IndentIfShouldFormat() : string.Empty) + line)) +
            (vhdlGenerationOptions.FormatCode ? string.Empty : " ");
}
