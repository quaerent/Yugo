using Microsoft.Xna.Framework;
using Yugo.Core.Game;
using Yugo.Core.Serialization;

namespace Yugo.Core.Entities;

/// <summary>
/// Base class for all entities that belong to a color cluster.
/// </summary>
public abstract class ClusterEntity : Entity
{
    public int ClusterId { get; set; }

    protected ClusterEntity(Level level)
        : base(level)
    {
        ClusterId = 1;
    }

    protected ClusterEntity(Level level, IEnumerable<Point> occupiedCells, int clusterId)
        : base(level, occupiedCells)
    {
        ClusterId = clusterId;
    }

    public override void Serialize(Dictionary<string, string> data)
    {
        base.Serialize(data);
        data["clusterId"] = ClusterId.ToString();
    }

    public override void Deserialize(Dictionary<string, string> data)
    {
        base.Deserialize(data);
        if (data.TryGetValue("clusterId", out var raw) && int.TryParse(raw, out var value))
        {
            ClusterId = value;
        }
    }
}
