using Yugo.Core.Entities;
using Yugo.Core.Serialization;

namespace Yugo.Core.Rules;

[TypeId("merge-movable", "Movable Merge")]
public class ClusterMergeRule : AdjacencyMergeRule<ClusterEntity>
{
    protected override bool IsMergeable(ClusterEntity a, ClusterEntity b)
    {
        return a.ClusterId == b.ClusterId;
    }

    protected override int GetPriority(ClusterEntity entity)
    {
        return entity is Piston ? 1 : 0;
    }
}
