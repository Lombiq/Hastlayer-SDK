using System;
using System.Collections.Generic;
using System.Reflection;

namespace Hast.Transformer.Abstractions.Extensions
{
    public static class AssembliesEnumerableExtensions
    {
        public static void ThrowArgumentExceptionIfAnyInMemory(this IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                if (string.IsNullOrEmpty(assembly.Location))
                {
                    throw new ArgumentException(
                        "No assembly used for hardware generation can be an in-memory one, but the assembly named \"" +
                        assembly.FullName + "\" is.");
                }
            }
        }
    }
}
