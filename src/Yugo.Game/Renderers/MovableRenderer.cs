using Microsoft.Xna.Framework;
using Yugo.Core.Entities;

namespace Yugo.Game.Renderer;

public sealed class MovableRenderer : IRenderer
{
    private readonly Scene _scene;

    public MovableRenderer(Scene scene)
    {
        _scene = scene;
    }

    public void Render()
    {
        foreach (var movable in _scene.Level.Entities.OfType<Movable>())
        {
            var color = GetClusterColor(movable.ClusterId);
            _scene.DrawEntity(movable.OccupiedCells, color);
        }
    }

    private static Color GetClusterColor(int clusterId)
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
}
