namespace Yugo.Core.Primitives;

public readonly record struct HalfGridPoint(int X, int Y)
{
    public static HalfGridPoint operator +(HalfGridPoint left, HalfGridPoint right)
    {
        return new HalfGridPoint(left.X + right.X, left.Y + right.Y);
    }
}
