using Microsoft.Xna.Framework;
using Yugo.Core.Entities;

namespace Yugo.Game.Renderer;

public sealed class WallRenderer : IRenderer
{
    private readonly Scene _scene;

    public WallRenderer(Scene scene)
    {
        _scene = scene;
    }

    public void Render()
    {
        var color = new Color(50, 50, 50);
        foreach (var wall in _scene.Level.Entities.OfType<Wall>())
        {
            _scene.DrawEntity(wall.OccupiedCells, color);
        }
    }
}
