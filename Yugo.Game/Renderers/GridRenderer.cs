using Microsoft.Xna.Framework;

namespace Yugo.Game.Renderer;

public sealed class GridRenderer : IRenderer
{
    private readonly Scene _scene;

    public GridRenderer(Scene scene)
    {
        _scene = scene;
    }

    public void Render()
    {
        var metrics = _scene.CurrentGridMetrics;
        var grid = _scene.Level.Grid;

        var gridWidth = metrics.GridWidth;
        var gridHeight = metrics.GridHeight;
        var origin = metrics.Origin;
        var halfUnitPixels = metrics.HalfUnitPixels;

        // Draw full-cell grid lines (1 cell = 2 half-units)
        for (int x = 0; x <= grid.Width; x += 2)
        {
            var xPixel = origin.X + x * halfUnitPixels;
            _scene.DrawRect(
                new Rectangle(xPixel, origin.Y - gridHeight, 1, gridHeight),
                Color.Black * 0.35f
            );
        }

        for (int y = 0; y <= grid.Height; y += 2)
        {
            var yPixel = origin.Y - y * halfUnitPixels;
            _scene.DrawRect(new Rectangle(origin.X, yPixel, gridWidth, 1), Color.Black * 0.35f);
        }
    }
}
