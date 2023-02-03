namespace Hast.VhdlBuilder.Representation.Expression;

/// <summary>
/// Represents a VHDL null statement (mostly used in case constructs).
/// </summary>
public class Null : IVhdlElement
{
    public static Null Instance { get; } = new Null();

    private Null()
    {
    }

    // It shouldn't be always terminated, so not terminating it here. See:
    // http://www.vhdl.renerta.com/mobile/source/vhd00045.htm
    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) => "null";
}
