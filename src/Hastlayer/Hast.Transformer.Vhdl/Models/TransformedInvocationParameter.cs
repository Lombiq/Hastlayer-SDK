using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;

namespace Hast.Transformer.Vhdl.Models;

public class TransformedInvocationParameter
{
    public IVhdlElement Reference { get; set; }
    public DataType DataType { get; set; }
}
