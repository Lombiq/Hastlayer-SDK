using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation.Declaration;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hast.VhdlBuilder.Representation.Expression;

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class Invocation : IVhdlElement
{
    public IVhdlElement Target { get; set; }
    public IList<IVhdlElement> Parameters { get; } = new List<IVhdlElement>();

    public Invocation()
    {
    }

    public Invocation(IVhdlElement target, params IVhdlElement[] parameters)
    {
        Target = target;
        Parameters = parameters.ToList();
    }

    public Invocation(string targetId, params IVhdlElement[] parameters)
    {
        Target = targetId.ToVhdlIdValue();
        Parameters = parameters.ToList();
    }

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Target.ToVhdl(vhdlGenerationOptions) +
        (Parameters != null && Parameters.Any() ? "(" + Parameters.ToVhdl(vhdlGenerationOptions, ", ", string.Empty) + ")" : string.Empty);

    public static Invocation ToInteger(IVhdlElement value) => new("to_integer", value);

    public static Invocation ToReal(IVhdlElement value) => new("real", value);

    public static Invocation Resize(IVhdlElement value, int size) => InvokeSizingFunction("resize", value, size);

    public static Invocation ToSigned(IVhdlElement value, int size) => InvokeSizingFunction("to_signed", value, size);

    public static Invocation ToUnsigned(IVhdlElement value, int size) => InvokeSizingFunction("to_unsigned", value, size);

    public static Invocation InvokeSizingFunction(string functionName, IVhdlElement value, int size) =>
        new(functionName.ToVhdlIdValue(), value, size.ToVhdlValue(KnownDataTypes.UnrangedInt));
}

[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
public class NamedInvocationParameter : IVhdlElement
{
    public INamedElement FormalParameter { get; set; }
    public INamedElement ActualParameter { get; set; }

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        vhdlGenerationOptions.ShortenName(FormalParameter.Name) + " => " + vhdlGenerationOptions.ShortenName(ActualParameter.Name);
}
