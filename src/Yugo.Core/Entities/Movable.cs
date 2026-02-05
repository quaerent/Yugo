using Microsoft.Xna.Framework;
using Yugo.Core.Game;

namespace Yugo.Core.Entities;

public class Movable : Entity
{
    public int ClusterId;

    public Movable(Level level, IEnumerable<Point> occupiedCells, int clusterId)
        : base(level, occupiedCells)
    {
        ClusterId = clusterId;
    }

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
            Level.Entities.Remove(this);
            foreach (var cell in OccupiedCells)
            {
                if (!Grid.IsInside(cell))
                    continue;
                Grid[cell] = null;
            }
        }
        else if (maxFreeMove > 0)
        {
            // fall to the ground
            var vec = Level.Gravity.ToPoint();
            Translate(new Point(vec.X * maxFreeMove, vec.Y * maxFreeMove));
            Level.TryPush(this, Level.Gravity);
        }
    }
}
