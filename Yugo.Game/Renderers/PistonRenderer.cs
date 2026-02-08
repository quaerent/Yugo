using Microsoft.Xna.Framework;
using Yugo.Core.Entities;
using Yugo.Game.Renderer;

namespace Yugo.Game.Renderers;

public sealed class PistonRenderer : IRenderer
{
    private readonly Scene _scene;

    public PistonRenderer(Scene scene)
    {
        _scene = scene;
    }

    public void Render()
    {
        var highlighted = _scene.GetHighlightedEntity();
        var color = RenderUtil.GetClusterColor(100);
        foreach (var piston in _scene.Level.Entities.OfType<Piston>())
        {
            var drawColor = piston == highlighted ? RenderUtil.GetHighlightedColor(color) : color;
            _scene.DrawEntity(piston.OccupiedCells, drawColor);
        }
    }
}
