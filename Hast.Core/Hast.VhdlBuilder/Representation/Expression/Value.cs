using Hast.VhdlBuilder.Representation.Declaration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hast.VhdlBuilder.Representation.Expression;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Value : IVhdlElement
{
    // These below need to be Lazy, because otherwise there would be a circular dependency between the static ctors with
    // KnownDataTypes (since it uses ToVhdlValue() and thus this class a lot for default values).
    private static readonly Lazy<Value> _trueLazy = new(() => new Value { DataType = KnownDataTypes.Boolean, Content = "true" });

    public static Value True => _trueLazy.Value;
    private static readonly Lazy<Value> _falseLazy = new(() => new Value { DataType = KnownDataTypes.Boolean, Content = "false" });
    public static Value False => _falseLazy.Value;
    private static readonly Lazy<Value> _zeroCharacterLazy = new(() => new Character('0'));
    public static Value ZeroCharacter => _zeroCharacterLazy.Value;
    private static readonly Lazy<Value> _oneCharacterLazy = new(() => new Character('1'));
    public static Value OneCharacter => _oneCharacterLazy.Value;

    public DataType DataType { get; set; }
    public string Content { get; set; }
    public IVhdlElement EvaluatedContent { get; set; }

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions)
    {
        var content = EvaluatedContent != null ? EvaluatedContent.ToVhdl(vhdlGenerationOptions) : Content;

        if (DataType == null) return content;

        // Handling signed and unsigned types specially.
        if (KnownDataTypes.Integers.Contains(DataType))
        {
            // Using ToVhdlValue() on content would cause a stack overflow, since this would be called again. So this
            // needs to be the root of the Value callchain.
            var value = new Raw(content);
            var size = ((SizedDataType)DataType).SizeNumber;

            return (DataType.Name == KnownDataTypes.UInt32.Name ?
                Invocation.ToUnsigned(value, size) :
                Invocation.ToSigned(value, size)).ToVhdl(vhdlGenerationOptions);
        }

        if (DataType == KnownDataTypes.Real) return Invocation.ToReal(new Raw(content)).ToVhdl(vhdlGenerationOptions);

        if (DataType.TypeCategory is DataTypeCategory.Scalar or DataTypeCategory.Unit) return content;

        if (DataType.TypeCategory == DataTypeCategory.Identifier)
        {
            // This is how the source should look.
#pragma warning disable CA1308 // Normalize strings to uppercase
            return vhdlGenerationOptions.ShortenName(DataType == KnownDataTypes.Boolean ? content.ToLowerInvariant() : content);
#pragma warning restore CA1308 // Normalize strings to uppercase
        }

        if (DataType.TypeCategory == DataTypeCategory.Array)
        {
            return ToVhdlArray(content, vhdlGenerationOptions);
        }

        if (DataType.TypeCategory == DataTypeCategory.Character) return "'" + content + "'";

        return content;
    }

    private string ToVhdlArray(string content, IVhdlGenerationOptions vhdlGenerationOptions)
    {
        if (content.Contains("others =>", StringComparison.InvariantCulture))
        {
            return $"({content})";
        }

        if (DataType.IsLiteralArrayType())
        {
            return $"\"{content}\"";
        }

        if (EvaluatedContent is IBlockElement blockElement)
        {
            content = blockElement.Body.ToVhdl(vhdlGenerationOptions, ", ", string.Empty);
        }

        return $"({content})";
    }

    public static IVhdlElement UnrangedInt(int value) => new Value
    {
        DataType = KnownDataTypes.UnrangedInt,
        Content = value.ToTechnicalString(),
    };
}
