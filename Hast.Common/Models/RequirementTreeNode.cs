using Hast.Common.Interfaces;
using System.Collections.Generic;

namespace Hast.Common.Models
{
    public class RequirementTreeNode<TItem, TKey>
        where TItem : IRequirement<TKey>
    {
        public TItem Data { get; set; }
        public List<RequirementTreeNode<TItem, TKey>> Children { get; } = new List<RequirementTreeNode<TItem, TKey>>();
    }
}
