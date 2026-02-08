using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Yugo.Core.Rules;
using Yugo.Core.Serialization;
using Yugo.Game.Screens;

namespace Yugo.Game;

public class Engine : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;

    private IScreen _currentScreen = null!;
    private ImGuiRenderer _imGuiRenderer = null!;
    private RemoteService _remoteService = null!;
    private readonly System.Collections.Concurrent.ConcurrentQueue<Action> _remoteCommands = new();

    // In-memory cache for cloud level titles synced from Web
    private readonly Dictionary<int, string> _cloudTitleCache = new();

    public record UserInfo(int Id, string Username, bool IsAdmin);

    public UserInfo? CurrentUser { get; set; }

    public SpriteBatch SpriteBatch => _spriteBatch;
    public Texture2D Pixel => _pixel;
    public ImGuiRenderer ImGuiRenderer => _imGuiRenderer;
    public RemoteService RemoteService => _remoteService;
    public IScreen CurrentScreen => _currentScreen;

    public Engine()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _imGuiRenderer = new ImGuiRenderer(this);
        _imGuiRenderer.RebuildFontAtlas();
        _remoteService = new RemoteService(this);
        _remoteService.Start();
        LoadMenu();
    }

    public void OnRemoteCommand(Action action) => _remoteCommands.Enqueue(action);

    protected override void Update(GameTime gameTime)
    {
        while (_remoteCommands.TryDequeue(out var cmd))
            cmd();
        base.Update(gameTime);
        _currentScreen.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.White);
        _spriteBatch.Begin();
        _currentScreen.Render();
        _spriteBatch.End();
        _imGuiRenderer.BeforeLayout(gameTime);
        _currentScreen.DrawGui();
        _imGuiRenderer.AfterLayout();
        base.Draw(gameTime);
    }

    public void LoadMenu() =>
        _currentScreen = new MenuScreen(this, PersistentSettings.LoadRecentLevels());

    public void LoadGameplay(LevelIdentity identity)
    {
        RegisterRecentLevel(identity);
        _currentScreen = new Scene(this, identity);
    }

    public void LoadEditor(LevelIdentity identity)
    {
        RegisterRecentLevel(identity);
        _currentScreen = new EditorScreen(this, LevelXml.Load(identity.LocalPath), identity);
    }

    public void LoadEditorNew()
    {
        var level = new Yugo.Core.Game.Level(
            30,
            18,
            new IMergeRule[] { new ClusterMergeRule() },
            new IWinRule[] { new MovableWinRule() }
        );
        var identity = new LevelIdentity("", "New Level");
        _currentScreen = new EditorScreen(this, level, identity);
    }

    public void RegisterRecentLevel(LevelIdentity identity)
    {
        if (string.IsNullOrEmpty(identity.LocalPath))
            return;
        var list = PersistentSettings.LoadRecentLevels();
        list.RemoveAll(e =>
            e.LocalPath == identity.LocalPath || (identity.IsCloud && e.CloudId == identity.CloudId)
        );
        list.Insert(0, identity);
        PersistentSettings.SaveRecentLevels(list.Take(10).ToList());
    }

    public void RemoveRecentLevel(string path)
    {
        var list = PersistentSettings.LoadRecentLevels();
        list.RemoveAll(e => e.LocalPath == path);
        PersistentSettings.SaveRecentLevels(list);
    }

    public string GetDisplayTitle(LevelIdentity identity)
    {
        if (identity.IsCloud && identity.CloudId.HasValue)
        {
            if (_cloudTitleCache.TryGetValue(identity.CloudId.Value, out var title))
                return title;
            return $"Cloud Level #{identity.CloudId}";
        }
        return identity.Title ?? Path.GetFileNameWithoutExtension(identity.LocalPath);
    }

    public void SyncCloudTitles(Dictionary<int, string> titles)
    {
        foreach (var (id, title) in titles)
        {
            _cloudTitleCache[id] = title;
        }
        Console.WriteLine($"[Engine] Synced {titles.Count} cloud titles.");
    }
}
