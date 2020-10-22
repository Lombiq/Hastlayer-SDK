using System;

namespace Hast.Synthesis.Abstractions
{
    public class ReplaceableAttribute : Attribute
    {
        public static readonly string Name = nameof(ReplaceableAttribute).Replace(nameof(Attribute), string.Empty);

        public string Key { get; }

        public ReplaceableAttribute(string key)
        {
            Key = key;
        }
    }
}
