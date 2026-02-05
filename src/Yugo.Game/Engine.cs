using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Yugo.Game;

public sealed class Engine : Microsoft.Xna.Framework.Game
{
    public static Engine Instance { get; private set; } = null!;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;
    private SpriteFont _uiFont = null!;

    public Scene CurrentScene { get; private set; } = null!;

    public Engine()
    {
        if (Instance != null)
            throw new InvalidOperationException("Engine singleton already created.");

        Instance = this;
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    public SpriteBatch SpriteBatch => _spriteBatch;
    public Texture2D Pixel => _pixel;
    public SpriteFont UiFont => _uiFont;

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _uiFont = Content.Load<SpriteFont>("Fonts/UiFont");

        var levelPath = Path.Combine(AppContext.BaseDirectory, "Content", "Levels", "default.xml");
        CurrentScene = new Scene(this, levelPath);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        CurrentScene.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        var background = CurrentScene.CurrentEndState switch
        {
            Scene.EndState.Win => new Color(233, 247, 239),
            _ => Color.White,
        };

        GraphicsDevice.Clear(background);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        CurrentScene.Render();
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
