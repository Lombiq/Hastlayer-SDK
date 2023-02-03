namespace Hast.VhdlBuilder.Representation;

/// <summary>
/// Represents a no-op VHDL element.
/// </summary>
public class Empty : IVhdlElement
{
    public static Empty Instance { get; } = new Empty();

    private Empty()
    {
    }

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) => string.Empty;
}
