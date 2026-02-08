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
        var highlighted = _scene.GetHighlightedEntity();
        foreach (var movable in _scene.Level.Entities.OfType<Movable>())
        {
            var color = RenderUtil.GetClusterColor(movable.ClusterId);
            if (movable == highlighted)
            {
                color = RenderUtil.GetHighlightedColor(color);
            }
            _scene.DrawEntity(movable.OccupiedCells, color);
        }
    }
}
