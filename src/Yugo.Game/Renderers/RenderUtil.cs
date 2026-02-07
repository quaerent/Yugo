using Microsoft.Xna.Framework;

namespace Yugo.Game.Renderer;

public static class RenderUtil
{
    public static Rectangle GetBounds(Scene scene, IEnumerable<Point> cells)
    {
        var cellList = cells as IList<Point> ?? cells.ToList();
        if (cellList.Count == 0)
            return Rectangle.Empty;

        var minX = cellList.Min(p => p.X);
        var maxX = cellList.Max(p => p.X);
        var minY = cellList.Min(p => p.Y);
        var maxY = cellList.Max(p => p.Y);

        var topLeft = scene.CellToRect(new Point(minX, maxY));
        var bottomRight = scene.CellToRect(new Point(maxX, minY));

        var width = bottomRight.Right - topLeft.Left;
        var height = bottomRight.Bottom - topLeft.Top;
        return new Rectangle(topLeft.Left, topLeft.Top, width, height);
    }

    public static Color GetClusterColor(int clusterId)
    {
        return clusterId switch
        {
            1 => new Color(242, 117, 96),
            2 => new Color(122, 206, 255),
            3 => new Color(145, 220, 160),
            4 => new Color(242, 200, 90),
            5 => new Color(176, 140, 255),
            _ => new Color(200, 200, 200),
        };
    }

    public static void DrawEntityOutline(
        Scene scene,
        IEnumerable<Point> cells,
        Color color,
        int thickness
    )
    {
        var cellSet = cells as HashSet<Point> ?? [.. cells];

        foreach (var cell in cellSet)
        {
            var rect = scene.CellToRect(cell);

            var hasLeft = cellSet.Contains(new Point(cell.X - 1, cell.Y));
            var hasRight = cellSet.Contains(new Point(cell.X + 1, cell.Y));
            var hasUp = cellSet.Contains(new Point(cell.X, cell.Y + 1));
            var hasDown = cellSet.Contains(new Point(cell.X, cell.Y - 1));

            if (!hasLeft)
                scene.DrawRect(new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            if (!hasRight)
                scene.DrawRect(
                    new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height),
                    color
                );
            if (!hasUp)
                scene.DrawRect(new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            if (!hasDown)
                scene.DrawRect(
                    new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness),
                    color
                );
        }
    }
}
