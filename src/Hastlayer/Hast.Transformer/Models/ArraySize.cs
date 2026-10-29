using System;
using System.Diagnostics;

namespace Hast.Transformer.Models;

[DebuggerDisplay("{ToString()}")]
// Used in ArraySizeHolder.
internal sealed class ArraySize : IArraySize
{
    public int Length { get; set; }

    public override string ToString() => "Length: " + Length.ToTechnicalString();
}
