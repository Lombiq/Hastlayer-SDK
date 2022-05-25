using Hast.Common.Interfaces;
using System.Collections.Generic;

namespace Hast.Common.Models;

public class RequirementTreeNode<TItem, TKey>
    where TItem : IRequirement<TKey>
{
    public TItem Data { get; set; }
    public IList<RequirementTreeNode<TItem, TKey>> Children { get; } = new List<RequirementTreeNode<TItem, TKey>>();
}
