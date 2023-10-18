using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Hast.Samples.Consumer.Attributes;

/// <summary>
/// Contains the hint text used to describe a configuration argument.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class HintAttribute : Attribute
{
    public string Text { get; }

    [SuppressMessage(
        "Design",
        "CA1019:Define accessors for attribute arguments",
        Justification = $"They are merged together into the {nameof(Text)} property.")]
    public HintAttribute(params string[] fragments) =>
        Text = string.Join(' ', fragments.Select(fragment => fragment.Trim()));
}
