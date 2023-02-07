using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Hast.VhdlBuilder.Testing;

public static class VhdlElementEnumerableExtensions
{
    public static bool ShouldContain<T>(this IEnumerable<IVhdlElement> elementsToCheck)
        where T : class, IVhdlElement => elementsToCheck.ShouldContain<T>(predicate: null);

    public static bool ShouldContain<T>(
        this IEnumerable<IVhdlElement> elementsToCheck,
        Expression<Func<T, bool>> predicate)
        where T : class, IVhdlElement
    {
        if (elementsToCheck.Contains(predicate)) return true;

        throw new VhdlStructureAssertionFailedException(
            $"The block of elements didn't contain any {typeof(T).Name} elements{(predicate == null ? "." : " matching " + predicate + ".")}",
            elementsToCheck.ToVhdl(VhdlGenerationOptions.Debug));
    }

    public static bool ShouldRecursivelyContain(
        this IEnumerable<IVhdlElement> elementsToCheck,
        Expression<Func<IVhdlElement, bool>> predicate) =>
        elementsToCheck.ShouldRecursivelyContain<IVhdlElement>(predicate);

    public static bool ShouldRecursivelyContain<T>(this IEnumerable<IVhdlElement> elementsToCheck)
        where T : class, IVhdlElement => elementsToCheck.ShouldRecursivelyContain<T>(predicate: null);

    public static bool ShouldRecursivelyContain<T>(
        this IEnumerable<IVhdlElement> elementsToCheck,
        Expression<Func<T, bool>> predicate)
        where T : class, IVhdlElement
    {
        var queue = new Queue<IVhdlElement>(elementsToCheck);

        while (queue.Count > 0)
        {
            var element = queue.Dequeue();

            if (element.Is(predicate)) return true;

            if (element is IBlockElement blockElement)
            {
                foreach (var subElement in blockElement.Body)
                {
                    queue.Enqueue(subElement);
                }
            }
        }

        throw new VhdlStructureAssertionFailedException(
            "The block of elements nor their children didn't contain any " + typeof(T).Name + " elements" +
            (predicate == null ? "." : " matching " + predicate + "."),
            elementsToCheck.ToVhdl(VhdlGenerationOptions.Debug));
    }

    public static bool Contains<T>(this IEnumerable<IVhdlElement> elements)
        where T : class, IVhdlElement => elements.Contains<T>(predicate: null);

    public static bool Contains<T>(this IEnumerable<IVhdlElement> elements, Expression<Func<T, bool>> predicate)
        where T : class, IVhdlElement =>
        elements.Any(element => element.Is(predicate));
}
