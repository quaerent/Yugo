using System.Linq;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Yugo.Game.Screens;

public sealed class MenuScreen : IScreen
{
    private const int LogoCell = 10;
    private const int LogoGap = 8;

    private readonly Engine _engine;
    private readonly IReadOnlyList<string> _recentLevels;
    private KeyboardState _previousKeyboard;

    public MenuScreen(Engine engine, IReadOnlyList<string> recentLevels)
    {
        _engine = engine;
        _recentLevels = recentLevels;
    }

    public void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        if (IsKeyPressed(keyboard, Keys.O))
        {
            var path = FileDialog.OpenLevelFile();
            if (!string.IsNullOrWhiteSpace(path))
                _engine.LoadGameplay(path);
        }
        else if (IsKeyPressed(keyboard, Keys.E))
        {
            var path = FileDialog.OpenLevelFile();
            if (!string.IsNullOrWhiteSpace(path))
                _engine.LoadEditor(path);
        }
        else if (IsKeyPressed(keyboard, Keys.N))
        {
            _engine.LoadEditorNew();
        }

        _previousKeyboard = keyboard;
    }

    public void Render()
    {
        DrawLogo();
    }

    public void DrawGui()
    {
        var viewport = _engine.GraphicsDevice.Viewport;
        // Position buttons below the logo
        ImGui.SetNextWindowPos(
            new System.Numerics.Vector2(viewport.Width / 2f, 220),
            ImGuiCond.Always,
            new System.Numerics.Vector2(0.5f, 0)
        );
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(450, 600));

        var flags =
            ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoBackground
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoSavedSettings;

        if (ImGui.Begin("MainMenu", flags))
        {
            ImGui.SetWindowFontScale(1.2f);

            if (ImGui.Button("OPEN LEVEL", new System.Numerics.Vector2(-1, 60)))
            {
                var path = FileDialog.OpenLevelFile();
                if (!string.IsNullOrWhiteSpace(path))
                    _engine.LoadGameplay(path);
            }

            if (ImGui.Button("EDIT LEVEL", new System.Numerics.Vector2(-1, 50)))
            {
                var path = FileDialog.OpenLevelFile();
                if (!string.IsNullOrWhiteSpace(path))
                    _engine.LoadEditor(path);
            }

            if (ImGui.Button("CREATE NEW", new System.Numerics.Vector2(-1, 50)))
            {
                _engine.LoadEditorNew();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextDisabled("RECENT LEVELS");

            if (ImGui.BeginChild("Recents", new System.Numerics.Vector2(-1, -30)))
            {
                foreach (var path in _recentLevels.ToList())
                {
                    var fileName = Path.GetFileName(path);
                    if (ImGui.Button(fileName, new System.Numerics.Vector2(280, 30)))
                    {
                        _engine.LoadGameplay(path);
                    }
                    ImGui.SameLine();
                    if (ImGui.Button($"Edit##{path}", new System.Numerics.Vector2(80, 30)))
                    {
                        _engine.LoadEditor(path);
                    }
                }
                ImGui.EndChild();
            }

            ImGui.SetCursorPosY(ImGui.GetWindowHeight() - 25);
            ImGui.TextDisabled("YUGO PUZZLE ENGINE v1.0");
            ImGui.End();
        }
    }

    private void DrawLogo()
    {
        var viewport = _engine.GraphicsDevice.Viewport;
        var letters = new[] { 'Y', 'U', 'G', 'O' };
        var totalWidth = letters.Length * 5 * LogoCell + (letters.Length - 1) * LogoGap;
        var startX = (viewport.Width - totalWidth) / 2;
        var startY = 40;

        var x = startX;
        foreach (var letter in letters)
        {
            if (LogoFont.TryGetValue(letter, out var rows))
            {
                for (int row = 0; row < rows.Length; row++)
                {
                    for (int col = 0; col < rows[row].Length; col++)
                    {
                        if (rows[row][col] == '1')
                        {
                            var rect = new Rectangle(
                                x + col * LogoCell,
                                startY + row * LogoCell,
                                LogoCell,
                                LogoCell
                            );
                            _engine.SpriteBatch.Draw(_engine.Pixel, rect, Color.Black);
                        }
                    }
                }
            }
            x += 5 * LogoCell + LogoGap;
        }
    }

    private bool IsKeyPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);
    }

    private static readonly Dictionary<char, string[]> LogoFont = new()
    {
        ['Y'] = ["10001", "10001", "01010", "00100", "00100", "00100", "00100"],
        ['U'] = ["10001", "10001", "10001", "10001", "10001", "10001", "01110"],
        ['G'] = ["01110", "10001", "10000", "10111", "10001", "10001", "01110"],
        ['O'] = ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
    };
}
