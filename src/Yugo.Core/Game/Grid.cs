using Microsoft.Xna.Framework;
using Yugo.Core.Entities;

namespace Yugo.Core.Game;

public class Grid
{
    public readonly int Width;
    public readonly int Height;
    private Entity?[,] _cells;

    public Grid(int width, int height)
    {
        Width = width;
        Height = height;
        _cells = new Entity?[width, height];
    }

    public Entity? this[Point point]
    {
        get => _cells[point.X, point.Y];
        set => _cells[point.X, point.Y] = value;
    }

    public Entity? this[int x, int y]
    {
        get => _cells[x, y];
        set => _cells[x, y] = value;
    }

    /// <summary>
    /// Checks if the given point is inside the grid bounds.
    /// </summary>
    /// <param name="point">The point to check.</param>
    /// <returns>True if the point is inside the grid; otherwise, false.</returns>
    public bool IsInside(Point point)
    {
        return IsInside(point.X, point.Y);
    }

    /// <summary>
    /// Checks if the given coordinates are inside the grid bounds.
    /// </summary>
    /// <param name="x">The x-coordinate to check.</param>
    /// <param name="y">The y-coordinate to check.</param>
    /// <returns>True if the coordinates are inside the grid; otherwise, false.</returns>
    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    /// <summary>
    /// Enumerates all points in the grid.
    /// </summary>
    /// <returns>An enumerable of all points within the grid.</returns>
    public IEnumerable<Point> Points()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                yield return new Point(x, y);
            }
        }
    }

    /// <summary>
    /// Enumerates all points and their corresponding entities in the grid.
    /// </summary>
    /// <returns>An enumerable of tuples containing points and their corresponding entities within the grid.</returns>
    public IEnumerable<(Point, Entity?)> Values()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                var point = new Point(x, y);
                var entity = _cells[x, y];
                yield return (point, entity);
            }
        }
    }
}
