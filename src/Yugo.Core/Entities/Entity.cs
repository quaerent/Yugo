using Yugo.Core.Primitives;

namespace Yugo.Core.Entities;

public abstract class Entity
{
    protected Entity(HalfGridRect bounds)
    {
        Bounds = bounds;
    }

    public HalfGridRect Bounds { get; protected set; }

    public abstract bool NeedsUpdate { get; }

    public abstract bool Move(MoveIntent intent);

    public abstract bool CanPush(Entity by, MoveIntent intent);

    public abstract bool Push(Entity by, MoveIntent intent);

    public abstract void Update();
}
