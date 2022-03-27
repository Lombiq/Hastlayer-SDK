using System;

namespace Hast.Synthesis.Abstractions;

/// <summary>
/// When a <see langword="static"/> <see langword="readonly"/> field is decorated with this attribute, its value (or
/// a replacement according to custom configuration) will be substituted at runtime the same way constants are
/// substituted in during compile time. See the "Using dynamic constants" section in "Docs/WorkingWithHastlayer.md"
/// for further information on usage.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ReplaceableAttribute : Attribute
{
    public static readonly string Name = "ReplaceableDynamicConstants";

    public string Key { get; }

    public ReplaceableAttribute(string key) => Key = key;
}
