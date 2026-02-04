namespace Yugo.Core.Primitives;

public readonly record struct MoveIntent(int DeltaX, int DeltaY)
{
    public static readonly MoveIntent Up = new(0, -1);
    public static readonly MoveIntent Down = new(0, 1);
    public static readonly MoveIntent Left = new(-1, 0);
    public static readonly MoveIntent Right = new(1, 0);
}
