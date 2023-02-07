using Hast.Transformer.Helpers;
using Hast.Transformer.Vhdl.Helpers;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using ICSharpCode.Decompiler.CSharp.Syntax;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System;
using System.Linq;

namespace Hast.Transformer.Vhdl.SubTransformers.ExpressionTransformers;

public class ArrayCreateExpressionTransformer : IArrayCreateExpressionTransformer
{
    private readonly ITypeConverter _typeConverter;

    public ArrayCreateExpressionTransformer(ITypeConverter typeConverter) => _typeConverter = typeConverter;

    public UnconstrainedArrayInstantiation CreateArrayInstantiation(
        ArrayCreateExpression expression,
        IVhdlTransformationContext context) =>
        ArrayHelper.CreateArrayInstantiation(
            _typeConverter.ConvertAstType(expression.Type, context),
            expression.GetStaticLength());

    public IVhdlElement Transform(ArrayCreateExpression expression, SubTransformerContext context)
    {
        if (expression.Arguments.Any() && expression.Arguments.Count != 1)
        {
            // For the sake of maximal compatibility with synthesis tools we don't allow multi-dimensional arrays, see:
            // http://vhdl.renerta.com/mobile/source/vhd00006.htm "Synthesis tools do generally not support
            // multidimensional arrays. The only exceptions to this are two-dimensional "vectors of vectors". Some
            // synthesis tools allow two-dimensional arrays."
            ExceptionHelper.ThrowOnlySingleDimensionalArraysSupporterException(expression);
        }

        var length = expression.GetStaticLength();

        if (length < 1)
        {
            throw new InvalidOperationException(
                "An array should have a length greater than 1.".AddParentEntityName(expression));
        }

        var elementType = expression.GetElementType();
        var elementAstType = _typeConverter.ConvertAstType(elementType, context.TransformationContext);

        if (length > 500 && elementAstType is Record)
        {
            var message = StringHelper.Concatenate(
                $"You've created a large array (length: {length}) with non-primitive items (type: ",
                $"{elementType.GetFullName()}). The resulting hardware implementation might not fit on the FPGA ",
                $"and/or will take a very long time to complete. Consider using a smaller array (below 500 items).");
            context.Scope.Warnings.AddWarning("NonPrimitiveArrayTooLarge", message.AddParentEntityName(expression));
        }

        if (elementAstType.DefaultValue != null)
        {
            // Initializing the array with the .NET default values (so there are no surprises when reading values
            // without setting them previously).
            return ArrayTypeBase.CreateDefaultInitialization(
                ArrayHelper.CreateArrayInstantiation(elementAstType, length),
                elementAstType);
        }

        // If there's no default value then we can't initialize the array. This is the case when objects are stored in
        // the array and that's no problem, since objects are initialized during instantiation any way.
        return Empty.Instance;
    }
}
