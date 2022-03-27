using Hast.Layer;
using System.Diagnostics;

namespace Hast.Common.Models;

[DebuggerDisplay("{ToString()}")]
public class TransformationWarning : ITransformationWarning
{
    public string Code { get; set; }
    public string Message { get; set; }

    public override string ToString() => Code + " - " + Message;
}
