using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Expression;

namespace Hast.Transformer.Vhdl.Helpers;

internal static class ResizeHelper
{
    public const string SmartResizeName = "SmartResize";

    public static Invocation SmartResize(IVhdlElement value, int size) =>
        Invocation.InvokeSizingFunction(SmartResizeName, value, size);
}
