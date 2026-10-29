using Hast.Common.Interfaces;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Vhdl.SubTransformers.ExpressionTransformers;

/// <summary>
/// The result of <see cref="ITypeConversionTransformer.ImplementTypeConversion"/>.
/// </summary>
public interface ITypeConversionResult
{
    /// <summary>
    /// Gets the converted VHDL element.
    /// </summary>
    IVhdlElement ConvertedFromExpression { get; }

    /// <summary>
    /// Gets a value indicating whether the conversion incurred a loss of precision.
    /// </summary>
    bool IsLossy { get; }

    /// <summary>
    /// Gets a value indicating whether the expression value is array and is resized.
    /// </summary>
    bool IsResized { get; }
}

/// <summary>
/// The result of <see cref="ITypeConversionTransformer.ImplementTypeConversionForAssignment"/>.
/// </summary>
public interface IAssignmentTypeConversionResult : ITypeConversionResult
{
    /// <summary>
    /// Gets the data object being converted.
    /// </summary>
    IDataObject ConvertedToDataObject { get; }
}

/// <summary>
/// Transforms binary operations and data types so the usage fits the constraints of VHDL.
/// </summary>
public interface ITypeConversionTransformer : IDependency
{
    /// <summary>
    /// In VHDL the operands of binary operations should have the same type, so we need to do a type conversion if
    /// necessary.
    /// </summary>
    IVhdlElement ImplementTypeConversionForBinaryExpression(
        BinaryOperatorExpression binaryOperatorExpression,
        DataObjectReference variableReference,
        bool isLeft,
        SubTransformerContext context);

    /// <summary>
    /// Makes sure the value assignment has the correct matching type and in case of arrays the lengths must be matching
    /// too.
    /// </summary>
    IAssignmentTypeConversionResult ImplementTypeConversionForAssignment(
        DataType fromType,
        DataType toType,
        IVhdlElement fromExpression,
        IDataObject toDataObject);

    /// <summary>
    /// Matches up <paramref name="fromType"/> and <paramref name="toType"/> so they have the same type and length.
    /// </summary>
    ITypeConversionResult ImplementTypeConversion(DataType fromType, DataType toType, IVhdlElement fromExpression);
}
