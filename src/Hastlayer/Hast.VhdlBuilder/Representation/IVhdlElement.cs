namespace Hast.VhdlBuilder.Representation;

/// <summary>
/// Represents an entity in a VHDL source.
/// </summary>
/// <remarks>
/// <para>It's basically anything in VHDL.</para>
/// </remarks>
public interface IVhdlElement
{
    /// <summary>
    /// Returns the VHDL source code representing this element.
    /// </summary>
    /// <param name="vhdlGenerationOptions">Formatting configuration.</param>
    string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions);
}

public static class VhdlElementExtensions
{
    public static string ToVhdl(this IVhdlElement vhdlElement) => vhdlElement?.ToVhdl(new VhdlGenerationOptions());
}
