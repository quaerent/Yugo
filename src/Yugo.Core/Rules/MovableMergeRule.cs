using Yugo.Core.Entities;
using Yugo.Core.Serialization;

namespace Yugo.Core.Rules;

[TypeId("merge-movable", "Movable Merge")]
public class MovableMergeRule : AdjacencyMergeRule<Movable>
{
    protected override bool IsMergeable(Movable a, Movable b)
    {
        return a.ClusterId == b.ClusterId;
    }
}
