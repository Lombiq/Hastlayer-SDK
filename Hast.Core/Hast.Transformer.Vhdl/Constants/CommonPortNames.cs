using Hast.VhdlBuilder.Extensions;

namespace Hast.Transformer.Vhdl.Constants;

public static class CommonPortNames
{
    public static readonly string Clock = "Clock".ToExtendedVhdlId();
    public static readonly string Reset = "Reset".ToExtendedVhdlId();
    public static readonly string MemberId = "MemberId".ToExtendedVhdlId();
    public static readonly string Started = "Started".ToExtendedVhdlId();
    public static readonly string Finished = "Finished".ToExtendedVhdlId();
}
