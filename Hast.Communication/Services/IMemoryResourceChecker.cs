using Hast.Common.Interfaces;
using Hast.Communication.Models;
using Hast.Layer;
using Hast.Transformer.Abstractions.SimpleMemory;
using System.Collections.Generic;

namespace Hast.Communication.Services
{
    /// <summary>
    /// Checks if the memory resources can fit the <see cref="SimpleMemory"/> instance before sending it to the device.
    /// </summary>
    public interface IMemoryResourceChecker : IDependency
    {
        /// <summary>
        /// Verifies resource availability.
        /// </summary>
        /// <param name="memory">The memory we want to send to the device.</param>
        /// <param name="hardwareRepresentation">The representation of the device and program on it.</param>
        /// <returns>An object describing the problem.</returns>
        MemoryResourceProblem EnsureResourceAvailable(SimpleMemory memory, IHardwareRepresentation hardwareRepresentation);
    }

    public static class IMemoryResourceCheckerExtensions
    {
        public static MemoryResourceProblem Check(
            this IEnumerable<IMemoryResourceChecker> checkers,
            SimpleMemory memory,
            IHardwareRepresentation hardwareRepresentation)
        {
            foreach (var checker in checkers)
            {
                if (checker.EnsureResourceAvailable(memory, hardwareRepresentation) is { } problem) return problem;
            }

            return null;
        }
    }
}
