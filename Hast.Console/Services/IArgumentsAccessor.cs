using System.Collections.Generic;

namespace Hast.Console.Services
{
    /// <summary>
    ///  Holds the command line arguments.
    /// </summary>
    public interface IArgumentsAccessor
    {
        /// <summary>
        /// Gets the list of the command line arguments.
        /// </summary>
        List<string> Arguments { get; }
    }
}
