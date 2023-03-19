using Hast.VhdlBuilder.Representation.Declaration;
using System;
using System.Diagnostics;

namespace Hast.VhdlBuilder.Representation.Expression;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class DataTypeReference : DataType
{
    private readonly Func<IVhdlGenerationOptions, string> _vhdlGenerator;

    public DataTypeReference(DataType dataType, Func<IVhdlGenerationOptions, string> vhdlGenerator)
        : base(dataType) => _vhdlGenerator = vhdlGenerator;

    public override string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) => _vhdlGenerator(vhdlGenerationOptions);
}
