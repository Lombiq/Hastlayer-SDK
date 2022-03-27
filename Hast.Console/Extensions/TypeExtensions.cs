using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Hast.Console.Extensions;

public static class TypeExtensions
{
    public static IEnumerable<(Type Type, Attribute Attribute)> GetTypesWithAttribute(this Type attributeType) =>
        AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(
                assembly => assembly.GetTypes(),
                (_, type) => (Type: type, Attribute: type?.GetCustomAttribute(attributeType, inherit: true)))
            .Where(result => result.Type != null && result.Attribute != null)
            .ToList();
}
