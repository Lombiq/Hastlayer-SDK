using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation.Declaration;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;
using System;

namespace Hast.Transformer.Vhdl.Helpers;

internal static class ArrayHelper
{
    public static string CreateArrayTypeName(DataType elementType)
    {
        var elementSize = elementType.GetSize();

        return
            (elementType.Name.TrimExtendedVhdlIdDelimiters() +
            (elementSize != 0 ? elementSize.ToTechnicalString() : string.Empty) +
            "_Array")
            .ToExtendedVhdlId();
    }

    public static UnconstrainedArrayInstantiation CreateArrayInstantiation(DataType elementType, int length) =>
        new()
        {
            Name = CreateArrayTypeName(elementType),
            ElementType = elementType,
            RangeFrom = 0,
            RangeTo = length - 1,
        };

    public static void ThrowArraysCantBeNullIfArray(Expression expression)
    {
        if (expression.GetActualType().IsArray())
        {
            throw new NotSupportedException(
                "Arrays, unlike other objects, can't be compared to null and array references can't be assigned " +
                "null (see: https://github.com/Lombiq/Hastlayer-SDK/issues/16). Affected expression: " +
                expression.ToString().AddParentEntityName(expression));
        }
    }
}
