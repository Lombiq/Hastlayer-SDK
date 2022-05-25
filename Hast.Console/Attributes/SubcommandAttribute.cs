using System;

namespace Hast.Console.Attributes;

/// <summary>
/// Indicates that if the first positional argument is the <see cref="Name"/> then an instance of the decorated class
/// should take over from <see cref="Program"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SubcommandAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the subcommand to look for in the positional arguments.
    /// </summary>
    public string Name { get; }

    public SubcommandAttribute(string name) => Name = name;
}
