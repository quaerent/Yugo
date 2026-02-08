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
    private readonly List<LevelIdentity> _recentLevels;
    private KeyboardState _previousKeyboard;

    public MenuScreen(Engine engine, List<LevelIdentity> _recentLevels)
    {
        _engine = engine;
        this._recentLevels = _recentLevels;
    }

    public void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        _previousKeyboard = keyboard;
    }

    public void Render()
    {
        DrawLogo();
    }

    public void DrawGui()
    {
        var viewport = _engine.GraphicsDevice.Viewport;
        var posX = (int)(viewport.Width / 2f);
        var posY = 220;
        ImGui.SetNextWindowPos(
            new System.Numerics.Vector2(posX, posY),
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
            if (_engine.CurrentUser != null)
            {
                ImGui.TextDisabled($"CLOUD SESSION: {_engine.CurrentUser.Username.ToUpper()}");
            }
            else
            {
                ImGui.TextDisabled("NOT CONNECTED TO CLOUD (LOGIN ON WEB)");
            }
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Button("OPEN LEVEL", new System.Numerics.Vector2(-1, 60)))
            {
                var path = FileDialog.OpenLevelFile();
                if (!string.IsNullOrWhiteSpace(path))
                    _engine.LoadGameplay(
                        new LevelIdentity(Path.GetFileNameWithoutExtension(path), path)
                    );
            }

            if (ImGui.Button("EDIT LEVEL", new System.Numerics.Vector2(-1, 50)))
            {
                var path = FileDialog.OpenLevelFile();
                if (!string.IsNullOrWhiteSpace(path))
                    _engine.LoadEditor(
                        new LevelIdentity(Path.GetFileNameWithoutExtension(path), path)
                    );
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
                for (int i = 0; i < _recentLevels.Count; i++)
                {
                    var id = _recentLevels[i];

                    // Row layout: [X] [Title] [Edit]
                    ImGui.PushStyleColor(
                        ImGuiCol.Button,
                        new System.Numerics.Vector4(0.6f, 0.1f, 0.1f, 1.0f)
                    );
                    if (ImGui.Button($"X##del{i}", new System.Numerics.Vector2(25, 30)))
                    {
                        _engine.RemoveRecentLevel(id.LocalPath);
                        _engine.LoadMenu();
                    }
                    ImGui.PopStyleColor();

                    ImGui.SameLine();
                    string label = id.IsCloud ? $"[C] {id.Title}" : id.Title;
                    if (ImGui.Button($"{label}##btn{i}", new System.Numerics.Vector2(250, 30)))
                    {
                        _engine.LoadGameplay(id);
                    }

                    ImGui.SameLine();
                    if (ImGui.Button($"Edit##ed{i}", new System.Numerics.Vector2(60, 30)))
                    {
                        _engine.LoadEditor(id);
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

    private static readonly Dictionary<char, string[]> LogoFont = new()
    {
        ['Y'] = ["10001", "10001", "01010", "00100", "00100", "00100", "00100"],
        ['U'] = ["10001", "10001", "10001", "10001", "10001", "10001", "01110"],
        ['G'] = ["01110", "10001", "10000", "10111", "10001", "10001", "01110"],
        ['O'] = ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
    };
}
