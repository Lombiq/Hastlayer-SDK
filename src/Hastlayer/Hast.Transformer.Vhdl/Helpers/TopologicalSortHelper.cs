using System;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.Helpers;

// Taken from: http://www.codeproject.com/Articles/869059/Topological-sorting-in-Csharp
public static class TopologicalSortHelper
{
    public static IReadOnlyList<T> Sort<T>(IEnumerable<T> source, Func<T, IEnumerable<T>> getDependencies)
    {
        var sorted = new List<T>();
        var visited = new Dictionary<T, bool>();

        foreach (var item in source)
        {
            Visit(item, getDependencies, sorted, visited);
        }

        return sorted;
    }

    private static void Visit<T>(T item, Func<T, IEnumerable<T>> getDependencies, List<T> sorted, Dictionary<T, bool> visited)
    {
        var alreadyVisited = visited.TryGetValue(item, out var inProcess);

        if (alreadyVisited)
        {
            if (inProcess)
            {
                throw new ArgumentException("Cyclic dependency found.");
            }
        }
        else
        {
            var dependencies = getDependencies(item);
            if (dependencies != null)
            {
                visited[item] = true;
                foreach (var dependency in dependencies)
                {
                    Visit(dependency, getDependencies, sorted, visited);
                }
            }

            visited[item] = false;
            sorted.Add(item);
        }
    }
}
