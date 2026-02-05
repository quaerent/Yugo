using Microsoft.Xna.Framework;
using Yugo.Core.Game;

namespace Yugo.Core.Entities;

/// <summary>
/// A wall entity that cannot be moved or pushed.
/// </summary>

public class Wall : Entity
{
    public Wall(Level level, IEnumerable<Point> occupiedCells)
        : base(level, occupiedCells) { }

    public override void OnClick() { }

    public override void OnMove(Direction dir) { }

    public override IEnumerable<Point>? TryPush(Direction dir)
    {
        return null;
    }

    public override void OnPush(Direction dir) { }

    public override bool NeedsUpdate()
    {
        return false;
    }

    public override void Update() { }
}
