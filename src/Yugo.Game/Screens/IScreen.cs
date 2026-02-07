using Microsoft.Xna.Framework;

namespace Yugo.Game.Screens;

public interface IScreen
{
    void Update(GameTime gameTime);
    void Render();
    void DrawGui();
}
