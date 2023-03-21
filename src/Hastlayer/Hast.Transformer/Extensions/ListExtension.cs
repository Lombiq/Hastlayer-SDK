using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.IL.Transforms;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.Decompiler;

internal static class ListExtension
{
    // Since OR clauses can't be defined for a generic type parameter we need these overloads instead of a single
    // generic method.

    public static IList<IAstTransform> Remove<TRemove>(this IList<IAstTransform> list) =>
        RemoveInternal<IAstTransform, TRemove>(list);

    // Not all transform types are public so it's necessary to be able to find them by name too.
    public static IList<IAstTransform> Remove(this IList<IAstTransform> list, string className) =>
        RemoveInternal(list, className);

    public static IList<IILTransform> Remove<TRemove>(this IList<IILTransform> list) =>
        RemoveInternal<IILTransform, TRemove>(list);

    public static IList<IILTransform> Remove(this IList<IILTransform> list, string className) =>
        RemoveInternal(list, className);

    public static IList<IAstTransform> ReplaceWith<TReplace>(this IList<IAstTransform> list, IAstTransform replacement) =>
        ReplaceWithInternal<IAstTransform, TReplace>(list, replacement);

    public static IList<IILTransform> ReplaceWith<TReplace>(this IList<IILTransform> list, IILTransform replacement) =>
        ReplaceWithInternal<IILTransform, TReplace>(list, replacement);

    private static IList<TElement> RemoveInternal<TElement, TRemove>(IList<TElement> list)
    {
        list.Remove(list.Single(item => item.GetType() == typeof(TRemove)));
        return list;
    }

    private static IList<T> RemoveInternal<T>(IList<T> list, string className)
    {
        list.Remove(list.Single(item => item.GetType().Name == className));
        return list;
    }

    private static IList<TElement> ReplaceWithInternal<TElement, TReplace>(IList<TElement> list, TElement replacement)
    {
        list[list.IndexOf(list.Single(item => item.GetType() == typeof(TReplace)))] = replacement;
        return list;
    }
}
