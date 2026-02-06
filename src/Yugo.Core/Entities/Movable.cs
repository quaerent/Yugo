using Microsoft.Xna.Framework;
using Yugo.Core.Game;
using Yugo.Core.Serialization;

namespace Yugo.Core.Entities;

[TypeId("movable", "Movable Block")]
public class Movable(Level level, IEnumerable<Point> occupiedCells, int clusterId)
    : Entity(level, occupiedCells)
{
    public int ClusterId = clusterId;

    public Movable(Level level)
        : this(level, [], -1) { }

    public override void OnClick() { }

    public override void OnMove(Direction dir)
    {
        if (!dir.PerpendicularTo(Level.Gravity))
            return;

        // Up + LR + LR + Down
        Level.TryPush(this, Level.Gravity.Opposite());
        Level.TryPush(this, dir);
        Level.TryPush(this, dir);
        Level.TryPush(this, Level.Gravity);
    }

    public override IEnumerable<Point>? TryPush(Direction dir)
    {
        return OccupiedCells.Select(p => p + dir.ToPoint());
    }

    public override void OnPush(Direction dir) { }

    public override bool NeedsUpdate()
    {
        return GetObstacles(Level.Gravity).Count == 0;
    }

    public override void Update()
    {
        var maxFreeMove = MaxFreeMove(Level.Gravity);
        if (maxFreeMove == int.MaxValue)
        {
            // fall out of the grid
            MarkDead();
        }
        else if (maxFreeMove > 0)
        {
            // fall to the ground
            var vec = Level.Gravity.ToPoint();
            Translate(new Point(vec.X * maxFreeMove, vec.Y * maxFreeMove));
            Level.TryPush(this, Level.Gravity);
        }
    }

    public override void Serialize(Dictionary<string, string> data)
    {
        base.Serialize(data);
        data["clusterId"] = ClusterId.ToString();
    }

    public override void Deserialize(Dictionary<string, string> data)
    {
        base.Deserialize(data);
        if (!data.TryGetValue("clusterId", out var raw))
            throw new InvalidOperationException("Missing required field 'clusterId'.");
        if (!int.TryParse(raw, out var value))
            throw new InvalidOperationException("Invalid integer for field 'clusterId'.");
        ClusterId = value;
    }
}
