namespace Yugo.Core.Primitives;

public readonly record struct HalfGridRect(HalfGridPoint Position, HalfGridPoint Size)
{
    public int Left => Position.X;
    public int Top => Position.Y;
    public int Right => Position.X + Size.X;
    public int Bottom => Position.Y + Size.Y;

    public bool Intersects(HalfGridRect other)
    {
        return Left < other.Right && Right > other.Left && Top < other.Bottom && Bottom > other.Top;
    }
}
