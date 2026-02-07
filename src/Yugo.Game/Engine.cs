using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Yugo.Core.Game;
using Yugo.Core.Rules;
using Yugo.Core.Serialization;
using Yugo.Game.Screens;

namespace Yugo.Game;

public sealed class Engine : Microsoft.Xna.Framework.Game
{
    public static Engine Instance { get; private set; } = null!;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;
    private SpriteFont _uiFont = null!;
    private readonly List<string> _recentLevels;
    private IScreen _currentScreen = null!;
    private ImGuiRenderer _imGuiRenderer = null!;

    public Engine()
    {
        if (Instance != null)
            throw new InvalidOperationException("Engine singleton already created.");

        Instance = this;
        _recentLevels = PersistentSettings.LoadRecentLevels();
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += (_, _) =>
        {
            _currentScreen?.Update(new GameTime());
        };
    }

    public SpriteBatch SpriteBatch => _spriteBatch;
    public Texture2D Pixel => _pixel;
    public SpriteFont UiFont => _uiFont;
    public ImGuiRenderer ImGuiRenderer => _imGuiRenderer;

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _uiFont = Content.Load<SpriteFont>("Fonts/UiFont");

        SnapshotFactory.EnsureRegistered(typeof(Engine).Assembly);
        SnapshotFactory.EnsureRegistered(typeof(Level).Assembly);

        _imGuiRenderer = new ImGuiRenderer(this);
        _imGuiRenderer.RebuildFontAtlas();

        LoadMenu();
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        _currentScreen.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        var background = Color.White;
        if (_currentScreen is Scene scene)
        {
            background = scene.CurrentEndState switch
            {
                Scene.EndState.Win => new Color(233, 247, 239),
                _ => Color.White,
            };
        }

        GraphicsDevice.Clear(background);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _currentScreen.Render();
        _spriteBatch.End();

        // Render ImGui UI on top
        _imGuiRenderer.BeforeLayout(gameTime);
        _currentScreen.DrawGui();
        _imGuiRenderer.AfterLayout();

        base.Draw(gameTime);
    }

    public void LoadMenu()
    {
        _currentScreen = new MenuScreen(this, _recentLevels);
    }

    public void LoadGameplay(string levelPath)
    {
        if (string.IsNullOrWhiteSpace(levelPath))
            return;

        levelPath = Path.GetFullPath(levelPath);
        RegisterRecentLevel(levelPath);
        _currentScreen = new Scene(this, levelPath);
    }

    public void LoadEditor(string levelPath)
    {
        if (string.IsNullOrWhiteSpace(levelPath))
            return;

        levelPath = Path.GetFullPath(levelPath);
        RegisterRecentLevel(levelPath);
        _currentScreen = new EditorScreen(this, LevelXml.Load(levelPath), levelPath);
    }

    public void LoadEditorNew()
    {
        var level = CreateNewLevel();
        _currentScreen = new EditorScreen(this, level, null);
    }

    public void RegisterRecentLevel(string levelPath)
    {
        _recentLevels.RemoveAll(p =>
            string.Equals(p, levelPath, StringComparison.OrdinalIgnoreCase)
        );
        _recentLevels.Insert(0, levelPath);
        if (_recentLevels.Count > 10)
            _recentLevels.RemoveRange(10, _recentLevels.Count - 10);

        PersistentSettings.SaveRecentLevels(_recentLevels);
    }

    private static Level CreateNewLevel()
    {
        var mergeRules = new IMergeRule[] { new ClusterMergeRule() };
        var winRules = new IWinRule[] { new MovableWinRule() };
        return new Level(30, 18, mergeRules, winRules);
    }
}
