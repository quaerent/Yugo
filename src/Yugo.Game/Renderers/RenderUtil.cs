using Microsoft.Xna.Framework;

namespace Yugo.Game.Renderer;

public static class RenderUtil
{
    public static void DrawEntityOutline(
        Scene scene,
        IEnumerable<Point> cells,
        Color color,
        int thickness
    )
    {
        var cellSet = cells as HashSet<Point> ?? new HashSet<Point>(cells);

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
