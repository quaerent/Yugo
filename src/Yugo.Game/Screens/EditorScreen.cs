using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Yugo.Core.Entities;
using Yugo.Core.Game;
using Yugo.Core.Serialization;
using Yugo.Game.Renderer;

namespace Yugo.Game.Screens;

public sealed class EditorScreen : IScreen
{
    private readonly Engine _engine;
    private Level _level;
    private readonly GridView _gridView;
    private string? _levelPath;
    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;
    private Point? _lastPaintCell;
    private Tool _tool = Tool.Wall;
    private int _clusterId = 1;

    private int _newWidth = 30;
    private int _newHeight = 18;
    private bool _showResizeWarning = false;

    public EditorScreen(Engine engine, Level level, string? levelPath)
    {
        _engine = engine;
        _level = level;
        _levelPath = levelPath;
        _gridView = new GridView(engine, level);
        _newWidth = level.Grid.Width;
        _newHeight = level.Grid.Height;
    }

    public void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();

        if (IsKeyPressed(keyboard, Keys.Escape))
            _engine.LoadMenu();
        if (IsKeyPressed(keyboard, Keys.S))
            SaveLevel();

        var viewport = _engine.GraphicsDevice.Viewport;
        // Adjust grid area to avoid the ImGui sidebar
        _gridView.Update(new Rectangle(250, 0, viewport.Width - 250, viewport.Height));

        var io = ImGui.GetIO();
        if (!io.WantCaptureMouse)
        {
            var leftDown = mouse.LeftButton == ButtonState.Pressed;
            var rightDown = mouse.RightButton == ButtonState.Pressed;

            if (leftDown || rightDown)
            {
                var cell = _gridView.ScreenToCell(mouse.Position);
                if (
                    cell.HasValue
                    && (!_lastPaintCell.HasValue || _lastPaintCell.Value != cell.Value)
                )
                {
                    if (rightDown)
                        EraseCell(cell.Value);
                    else
                        ApplyTool(cell.Value);
                    _lastPaintCell = cell.Value;
                }
            }
            else
            {
                _lastPaintCell = null;
            }
        }

        _previousMouse = mouse;
        _previousKeyboard = keyboard;
    }

    public void Render()
    {
        DrawGridLines();
        DrawEntities();
    }

    public void DrawGui()
    {
        var viewport = _engine.GraphicsDevice.Viewport;
        ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero);
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(250, viewport.Height));

        if (
            ImGui.Begin(
                "Editor Tools",
                ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse
            )
        )
        {
            ImGui.Text(
                string.IsNullOrWhiteSpace(_levelPath) ? "NEW LEVEL" : Path.GetFileName(_levelPath)
            );
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Button("SAVE (S)", new System.Numerics.Vector2(-1, 30)))
                SaveLevel();
            if (ImGui.Button("EXIT (Esc)", new System.Numerics.Vector2(-1, 30)))
                _engine.LoadMenu();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Text("LEVEL PROPERTIES");
            ImGui.InputInt("Width", ref _newWidth);
            ImGui.InputInt("Height", ref _newHeight);
            _newWidth = MathHelper.Clamp(_newWidth, 5, 100);
            _newHeight = MathHelper.Clamp(_newHeight, 5, 100);

            if (ImGui.Button("RESIZE LEVEL", new System.Numerics.Vector2(-1, 30)))
            {
                if (_level.Entities.Count > 0)
                    _showResizeWarning = true;
                else
                    PerformResize();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Text("TOOL SELECTION");

            int toolIdx = (int)_tool;
            string[] toolNames = ["Wall", "Movable", "Erase"];
            if (ImGui.Combo("Tool", ref toolIdx, toolNames, toolNames.Length))
            {
                _tool = (Tool)toolIdx;
            }

            if (_tool == Tool.Movable)
            {
                ImGui.InputInt("Cluster ID", ref _clusterId);
                _clusterId = MathHelper.Clamp(_clusterId, 1, 99);
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.TextDisabled("Status:");
            ImGui.TextUnformatted(
                _tool switch
                {
                    Tool.Wall => "Placing Walls",
                    Tool.Movable => $"Placing Cluster {_clusterId}",
                    _ => "Erasing",
                }
            );

            ImGui.End();
        }

        if (_showResizeWarning)
        {
            ImGui.OpenPopup("Confirm Resize");
        }

        if (
            ImGui.BeginPopupModal(
                "Confirm Resize",
                ref _showResizeWarning,
                ImGuiWindowFlags.AlwaysAutoResize
            )
        )
        {
            ImGui.TextColored(
                new System.Numerics.Vector4(1, 0, 0, 1),
                "WARNING: RESIZE WILL CLEAR ALL ENTITIES!"
            );
            ImGui.Text("Are you sure you want to proceed?");
            ImGui.Separator();

            if (ImGui.Button("CONFIRM", new System.Numerics.Vector2(120, 0)))
            {
                PerformResize();
                _showResizeWarning = false;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("CANCEL", new System.Numerics.Vector2(120, 0)))
            {
                _showResizeWarning = false;
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }

    private void PerformResize()
    {
        var mergeRules = _level.MergeRules;
        var winRules = _level.WinRules;
        var newLevel = new Level(_newWidth, _newHeight, mergeRules, winRules);

        _level = newLevel;
        _gridView.UpdateLevel(newLevel);

        _lastPaintCell = null;
    }

    private void DrawGridLines()
    {
        var metrics = _gridView.Metrics;
        var grid = _level.Grid;
        for (int x = 0; x <= grid.Width; x += 2)
        {
            var xPixel = metrics.Origin.X + x * metrics.HalfUnitPixels;
            _gridView.DrawRect(
                new Rectangle(xPixel, metrics.Origin.Y - metrics.GridHeight, 1, metrics.GridHeight),
                Color.Black * 0.2f
            );
        }
        for (int y = 0; y <= grid.Height; y += 2)
        {
            var yPixel = metrics.Origin.Y - y * metrics.HalfUnitPixels;
            _gridView.DrawRect(
                new Rectangle(metrics.Origin.X, yPixel, metrics.GridWidth, 1),
                Color.Black * 0.2f
            );
        }
    }

    private void DrawEntities()
    {
        foreach (var wall in _level.Entities.OfType<Wall>())
            _gridView.DrawEntity(wall.OccupiedCells, new Color(60, 60, 60));
        foreach (var movable in _level.Entities.OfType<Movable>())
            _gridView.DrawEntity(
                movable.OccupiedCells,
                RenderUtil.GetClusterColor(movable.ClusterId)
            );
    }

    private void ApplyTool(Point cell)
    {
        if (_tool == Tool.Erase)
        {
            EraseCell(cell);
            return;
        }

        // Only erase the specific cell from the existing entity
        EraseCell(cell);

        MergeIntoExisting(cell, _tool);
    }

    private void MergeIntoExisting(Point cell, Tool tool)
    {
        var neighbors = FindAdjacentEntities(cell, tool).ToList();
        if (neighbors.Count == 0)
        {
            if (tool == Tool.Wall)
                _level.AddEntity(new Wall(_level, [cell]));
            else
                _level.AddEntity(new Movable(_level, [cell], _clusterId));
            return;
        }
        var baseEntity = neighbors[0];
        var merged = new HashSet<Point>(baseEntity.OccupiedCells) { cell };
        foreach (var entity in neighbors.Skip(1))
        {
            foreach (var p in entity.OccupiedCells)
                merged.Add(p);
            _level.RemoveEntity(entity);
        }
        baseEntity.ReplaceCells(merged);
    }

    private IEnumerable<Entity> FindAdjacentEntities(Point cell, Tool tool)
    {
        var found = new HashSet<Entity>();
        foreach (
            var offset in new[]
            {
                new Point(1, 0),
                new Point(-1, 0),
                new Point(0, 1),
                new Point(0, -1),
            }
        )
        {
            var neighbor = cell + offset;
            if (!_level.Grid.IsInside(neighbor))
                continue;
            var occupant = _level.Grid[neighbor];
            if (occupant == null || found.Contains(occupant))
                continue;
            if (tool == Tool.Wall && occupant is Wall)
                found.Add(occupant);
            else if (tool == Tool.Movable && occupant is Movable m && m.ClusterId == _clusterId)
                found.Add(occupant);
        }
        return found;
    }

    private void EraseCell(Point cell)
    {
        var occupant = _level.Grid[cell];
        if (occupant == null)
            return;
        var remaining = occupant.OccupiedCells.Where(p => p != cell).ToList();
        if (remaining.Count == 0)
            _level.RemoveEntity(occupant);
        else
            occupant.ReplaceCells(remaining);
    }

    private void SaveLevel()
    {
        var path = _levelPath ?? FileDialog.SaveLevelFile();
        if (string.IsNullOrWhiteSpace(path))
            return;
        if (!path.EndsWith(".xml"))
            path += ".xml";
        LevelXml.Save(_level, path);
        _levelPath = path;
        _engine.RegisterRecentLevel(path);
    }

    private bool IsKeyPressed(KeyboardState current, Keys key) =>
        current.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);

    private enum Tool
    {
        Wall,
        Movable,
        Erase,
    }
}
