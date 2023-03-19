using System;
using System.Linq;

namespace Hast.Samples.Consumer.Attributes;

/// <summary>
/// Contains the hint text used to describe a configuration argument.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class HintAttribute : Attribute
{
    public string Text { get; }

    public HintAttribute(params string[] text) => Text = string.Join(" ", text.Select(fragment => fragment.Trim()));
}
