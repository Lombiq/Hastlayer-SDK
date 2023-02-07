using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Declaration;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Alias : TypedDataObjectBase
{
    /// <summary>
    /// Gets or sets the name of the object that the alias is created for.
    /// </summary>
    public IDataObject AliasedObject { get; set; }

    public Alias() => DataObjectKind = DataObjectKind.Variable;

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Terminated.Terminate(
            "Alias " +
            vhdlGenerationOptions.ShortenName(Name) +
            " : " +
            DataType.ToReference().ToVhdl(vhdlGenerationOptions) +
            " is " +
            AliasedObject.ToVhdl(vhdlGenerationOptions),
            vhdlGenerationOptions);
}
