using Yugo.Core.Entities;

namespace Yugo.Core.Rules;

public class MovableMergeRule : AdjacencyMergeRule<Movable>
{
    protected override bool IsMergeable(Movable a, Movable b)
    {
        return a.ClusterId == b.ClusterId;
    }
}
