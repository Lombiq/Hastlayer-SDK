using Hast.VhdlBuilder.Representation;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Vhdl.Models;

public class PartiallyTransformedBinaryOperatorExpression
{
    public BinaryOperatorExpression BinaryOperatorExpression { get; set; }
    public IVhdlElement LeftTransformed { get; set; }
    public IVhdlElement RightTransformed { get; set; }
}
