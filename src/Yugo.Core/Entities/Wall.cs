using Microsoft.Xna.Framework;
using Yugo.Core.Game;
using Yugo.Core.Serialization;

namespace Yugo.Core.Entities;

/// <summary>
/// A wall entity that cannot be moved or pushed.
/// </summary>
[TypeId("wall", "Wall")]
public class Wall(Level level, IEnumerable<Point> occupiedCells) : Entity(level, occupiedCells)
{
    public Wall(Level level)
        : this(level, []) { }

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
