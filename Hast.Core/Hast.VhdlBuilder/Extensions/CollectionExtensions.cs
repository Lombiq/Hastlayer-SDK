using Hast.VhdlBuilder.Representation.Declaration;
using System.Linq;

namespace System.Collections.Generic;

public static class CollectionExtensions
{
    /// <summary>
    /// Adds the given VHDL element to the collection if one with the same name doesn't exist in the collection already.
    /// </summary>
    public static void AddIfNew<T>(this ICollection<T> collection, T item)
        where T : INamedElement
    {
        if (collection.Any(element => element.Name == item.Name)) return;
        collection.Add(item);
    }
}
