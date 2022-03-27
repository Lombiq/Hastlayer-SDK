using Hast.Common.Interfaces;
using Hast.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Common.Extensions;

/// <summary>
/// Extension for ordering <see cref="IRequirement{T}"/> collections.
/// </summary>
public static class RequirementExtensions
{
    /// <summary>
    /// Returns a new collection where the items required by others are guaranteed to be prior to the ones requiring
    /// them. This way their load order will satisfy their requirements. Beside this, the order of items is an
    /// undefined implementation detail. If there are no requirements the <paramref name="source"/> order is kept.
    /// </summary>
    /// <param name="source">The original collection.</param>
    /// <typeparam name="TItem">The <see cref="IRequirement{T}"/> type of <paramref name="source"/>.</typeparam>
    /// <typeparam name="TKey">The <c>T</c> in <see cref="IRequirement{T}"/>.</typeparam>
    public static IReadOnlyCollection<TItem> OrderByRequirements<TItem, TKey>(this IEnumerable<TItem> source)
        where TItem : IRequirement<TKey>
        where TKey : IEquatable<TKey>
    {
        var items = source is IList<TItem> list ? list : source.ToList();
        var keys = items.Select(item => item.Name).ToList();
        CheckRequirements(items, keys);

        var descendantsByAncestor = items
            .SelectMany(item => item.Requirements.Select(requirement => new { item.Name, Ancestor = requirement }))
            .GroupBy(relationship => relationship.Ancestor)
            .ToDictionary(group => group.Key, group => group.Select(relationship => relationship.Name).ToList());
        var ancestors = descendantsByAncestor.SelectMany(pair => pair.Value).Distinct().ToHashSet();
        var roots = items
            .Where(item => !ancestors.Contains(item.Name))
            .Select(item => new RequirementTreeNode<TItem, TKey> { Data = item })
            .ToList();
        CreateForest(roots, descendantsByAncestor, items.ToDictionary(item => item.Name));
        PruneDuplicates(roots, new Dictionary<TKey, IList<RequirementTreeNode<TItem, TKey>>>());

        var results = new List<TItem>();
        BuildResults(results, roots);
        return results;
    }

    private static void CheckRequirements<TItem, TKey>(IList<TItem> items, IList<TKey> keys)
        where TItem : IRequirement<TKey>
    {
        // Look for impossible requirements.
        foreach (var item in items)
        {
            foreach (var requirement in item.Requirements)
            {
                if (!keys.Contains(requirement))
                {
                    throw new KeyNotFoundException($"The required service \"{requirement}\" was not found!");
                }
            }
        }
    }

    private static void CreateForest<TItem, TKey>(
        IList<RequirementTreeNode<TItem, TKey>> parents,
        IReadOnlyDictionary<TKey, List<TKey>> descendantsByAncestor,
        IReadOnlyDictionary<TKey, TItem> itemsDictionary)
        where TItem : IRequirement<TKey>
    {
        if (parents.Count == 0) return;

        foreach (var parent in parents)
        {
            if (descendantsByAncestor.TryGetValue(parent.Data.Name, out var descendants))
            {
                var subCollection = descendants
                    .Select(key => new RequirementTreeNode<TItem, TKey> { Data = itemsDictionary[key] });
                parent.Children.AddRange(subCollection);
            }

            CreateForest(parent.Children, descendantsByAncestor, itemsDictionary);
        }
    }

    private static void PruneDuplicates<TItem, TKey>(
        IList<RequirementTreeNode<TItem, TKey>> parents,
        IDictionary<TKey, IList<RequirementTreeNode<TItem, TKey>>> containersByName)
        where TItem : IRequirement<TKey>
        where TKey : IEquatable<TKey>
    {
        foreach (var parent in parents)
        {
            var key = parent.Data.Name;
            if (containersByName.TryGetValue(key, out var oldContainer))
            {
                oldContainer.RemoveAll(item => item.Data.Name.Equals(key));
            }

            containersByName[key] = parents;

            PruneDuplicates(parent.Children, containersByName);
        }
    }

    private static void BuildResults<TItem, TKey>(
        ICollection<TItem> results,
        IEnumerable<RequirementTreeNode<TItem, TKey>> parents)
        where TItem : IRequirement<TKey>
    {
        foreach (var parent in parents)
        {
            results.Add(parent.Data);
            BuildResults(results, parent.Children);
        }
    }
}
