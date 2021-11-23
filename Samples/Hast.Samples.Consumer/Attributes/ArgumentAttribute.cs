using System;
using System.Linq;

namespace Hast.Samples.Consumer.Attributes
{
    /// <summary>
    /// Represents a case-insensitive command line argument and any number of aliases for the given property.
    /// </summary>
    public class ArgumentAttribute : Attribute
    {
        public string[] Aliases { get; }

        public ArgumentAttribute(params string[] aliases) =>
            Aliases = aliases.Select(argument => argument.Trim().ToUpperInvariant()).ToArray();
    }
}
