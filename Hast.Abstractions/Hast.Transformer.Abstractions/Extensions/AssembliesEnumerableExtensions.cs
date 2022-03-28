using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Hast.Transformer.Abstractions.Extensions;

public static class AssembliesEnumerableExtensions
{
    public static void ThrowArgumentExceptionIfAnyInMemory(this IEnumerable<Assembly> assemblies)
    {
        var assembly = assemblies.FirstOrDefault(assembly => string.IsNullOrEmpty(assembly.Location));
        if (assembly != null)
        {
            throw new ArgumentException(
                "No assembly used for hardware generation can be an in-memory one, but the assembly named \"" +
                assembly.FullName + "\" is.");
        }
    }
}
