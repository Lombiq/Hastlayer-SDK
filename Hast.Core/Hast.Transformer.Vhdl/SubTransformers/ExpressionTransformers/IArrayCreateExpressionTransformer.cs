using Hast.Common.Interfaces;
using Hast.Transformer.Vhdl.Helpers;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Vhdl.SubTransformers.ExpressionTransformers;

/// <summary>
/// An array-specific sub-transformer used by <see cref="IExpressionTransformer"/>.
/// </summary>
public interface IArrayCreateExpressionTransformer : IDependency
{
    /// <summary>
    /// A shortcut for <see cref="ArrayHelper.CreateArrayInstantiation"/>.
    /// </summary>
    UnconstrainedArrayInstantiation CreateArrayInstantiation(
        ArrayCreateExpression expression,
        IVhdlTransformationContext context);

    /// <summary>
    /// Transforms the <see cref="ArrayCreateExpression"/> much like <see cref="IExpressionTransformer.Transform"/>.
    /// </summary>
    IVhdlElement Transform(ArrayCreateExpression expression, SubTransformerContext context);
}
