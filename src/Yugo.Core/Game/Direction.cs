using Microsoft.Xna.Framework;

namespace Yugo.Core.Game;

public enum Direction
{
    Up,
    Down,
    Left,
    Right,
}

public static class DirectionExtensions
{
    /// <summary>
    /// Converts a Direction to a Point representing the offset in that direction.
    /// </summary>
    /// <param name="direction">The direction to convert.</param>
    /// <returns>A Point representing the offset in the given direction.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static Point ToPoint(this Direction direction)
    {
        return direction switch
        {
            Direction.Up => new Point(0, 1),
            Direction.Down => new Point(0, -1),
            Direction.Left => new Point(-1, 0),
            Direction.Right => new Point(1, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };
    }

    /// <summary>
    /// Gets the opposite direction.
    /// </summary>
    /// <param name="direction">The direction to get the opposite of.</param>
    /// <returns>The opposite direction.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static Direction Opposite(this Direction direction)
    {
        return direction switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };
    }

    /// <summary>
    /// Determines if two directions are perpendicular to each other.
    /// </summary>
    /// <param name="dir1">The first direction.</param>
    /// <param name="dir2">The second direction.</param>
    /// <returns>True if the directions are perpendicular; otherwise, false.</returns>
    public static bool PerpendicularTo(this Direction dir1, Direction dir2)
    {
        return (dir1 == Direction.Up || dir1 == Direction.Down)
                && (dir2 == Direction.Left || dir2 == Direction.Right)
            || (dir1 == Direction.Left || dir1 == Direction.Right)
                && (dir2 == Direction.Up || dir2 == Direction.Down);
    }
}
