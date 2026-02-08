using Microsoft.Xna.Framework;
using Yugo.Core.Game;

namespace Yugo.Game;

public sealed class GridView
{
    private const int MinHalfUnitPixels = 6;
    private const int MarginPixels = 16;

    private readonly Engine _engine;
    private Level _level;
    private GridMetrics _metrics;

    public GridView(Engine engine, Level level)
    {
        _engine = engine;
        _level = level;
    }

    public GridMetrics Metrics => _metrics;

    public void UpdateLevel(Level level)
    {
        _level = level;
    }

    public void Update()
    {
        var viewport = _engine.GraphicsDevice.Viewport;
        Update(new Rectangle(0, 0, viewport.Width, viewport.Height));
    }

    public void Update(Rectangle area)
    {
        var availableWidth = Math.Max(1, area.Width - MarginPixels * 2);
        var availableHeight = Math.Max(1, area.Height - MarginPixels * 2);

        var halfUnitPixels = Math.Max(
            MinHalfUnitPixels,
            Math.Min(availableWidth / _level.Grid.Width, availableHeight / _level.Grid.Height)
        );

        var gridWidth = _level.Grid.Width * halfUnitPixels;
        var gridHeight = _level.Grid.Height * halfUnitPixels;

        var origin = new Point(
            area.X + (area.Width - gridWidth) / 2,
            area.Y + (area.Height + gridHeight) / 2
        );

        _metrics = new GridMetrics(origin, halfUnitPixels, gridWidth, gridHeight);
    }

    public Rectangle CellToRect(Point cell)
    {
        var origin = _metrics.Origin;
        var halfUnitPixels = _metrics.HalfUnitPixels;
        var x = origin.X + cell.X * halfUnitPixels;
        var y = origin.Y - (cell.Y + 1) * halfUnitPixels;
        return new Rectangle(x, y, halfUnitPixels, halfUnitPixels);
    }

    public Point? ScreenToCell(Point screen)
    {
        var origin = _metrics.Origin;
        var gridWidth = _metrics.GridWidth;
        var gridHeight = _metrics.GridHeight;
        var halfUnitPixels = _metrics.HalfUnitPixels;

        if (screen.X < origin.X || screen.X >= origin.X + gridWidth)
            return null;
        if (screen.Y > origin.Y || screen.Y <= origin.Y - gridHeight)
            return null;

        var x = (screen.X - origin.X) / halfUnitPixels;
        var y = (origin.Y - screen.Y - 1) / halfUnitPixels;

        var cell = new Point(x, y);
        return _level.Grid.IsInside(cell) ? cell : null;
    }

    public void DrawRect(Rectangle rect, Color color)
    {
        _engine.SpriteBatch.Draw(_engine.Pixel, rect, color);
    }

    public void DrawEntity(IEnumerable<Point> cells, Color color)
    {
        var cellSet = cells as HashSet<Point> ?? [.. cells];
        foreach (var cell in cellSet)
        {
            var rect = CellToRect(cell);

            var insetLeft = cellSet.Contains(new Point(cell.X - 1, cell.Y)) ? 0 : 1;
            var insetUp = cellSet.Contains(new Point(cell.X, cell.Y + 1)) ? 0 : 1;

            var x = rect.X + insetLeft;
            var y = rect.Y + insetUp;
            var width = rect.Width - insetLeft;
            var height = rect.Height - insetUp;

            if (width <= 0 || height <= 0)
                continue;

            DrawRect(new Rectangle(x, y, width, height), color);
        }
    }
}
