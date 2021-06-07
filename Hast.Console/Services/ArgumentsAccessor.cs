using System.Collections.Generic;

namespace Hast.Console.Services
{
    public class ArgumentsAccessor : IArgumentsAccessor
    {
        public List<string> Arguments { get; } = new();

        public ArgumentsAccessor(IEnumerable<string> arguments) => Arguments.AddRange(arguments);
    }
}
