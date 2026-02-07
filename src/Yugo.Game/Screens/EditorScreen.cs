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

    private Entity? _selectedEntity;
    private int _newWidth = 30;
    private int _newHeight = 18;
    private bool _showResizeWarning = false;
    private bool _isDirty = false;

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
        _gridView.Update(new Rectangle(300, 0, viewport.Width - 450, viewport.Height));

        var io = ImGui.GetIO();
        if (!io.WantCaptureMouse)
        {
            var leftDown = mouse.LeftButton == ButtonState.Pressed;
            var rightDown = mouse.RightButton == ButtonState.Pressed;
            var cell = _gridView.ScreenToCell(mouse.Position);

            if (_selectedEntity != null && cell.HasValue && (leftDown || rightDown))
            {
                if (!_lastPaintCell.HasValue || _lastPaintCell.Value != cell.Value)
                {
                    if (leftDown)
                        AddCellToEntity(_selectedEntity, cell.Value);
                    else
                        RemoveCellFromEntity(_selectedEntity, cell.Value);
                    _lastPaintCell = cell.Value;
                }
            }
            else if (
                mouse.LeftButton == ButtonState.Pressed
                && _previousMouse.LeftButton == ButtonState.Released
            )
            {
                if (cell.HasValue)
                    _selectedEntity = _level.Grid[cell.Value];
                else
                    _selectedEntity = null;
            }
            else
            {
                _lastPaintCell = null;
            }
        }

        _previousMouse = mouse;
        _previousKeyboard = keyboard;
    }

    private void AddCellToEntity(Entity entity, Point cell)
    {
        if (entity.OccupiedCells.Contains(cell))
            return;
        var occupant = _level.Grid[cell];
        if (occupant != null && occupant != entity)
            RemoveCellFromEntity(occupant, cell);
        entity.ReplaceCells(entity.OccupiedCells.Append(cell));
        _isDirty = true;
    }

    private void RemoveCellFromEntity(Entity entity, Point cell)
    {
        if (!entity.OccupiedCells.Contains(cell))
            return;
        var remaining = entity.OccupiedCells.Where(p => p != cell).ToList();
        if (remaining.Count == 0)
        {
            _level.RemoveEntity(entity);
            if (_selectedEntity == entity)
                _selectedEntity = null;
        }
        else
        {
            entity.ReplaceCells(remaining);
        }
        _isDirty = true;
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
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(300, viewport.Height));
        if (
            ImGui.Begin(
                "Inspector",
                ImGuiWindowFlags.NoMove
                    | ImGuiWindowFlags.NoResize
                    | ImGuiWindowFlags.NoCollapse
                    | ImGuiWindowFlags.NoSavedSettings
            )
        )
        {
            DrawHeader();
            ImGui.Separator();

            if (ImGui.CollapsingHeader("Level Settings", ImGuiTreeNodeFlags.DefaultOpen))
                DrawLevelSettings();

            ImGui.Spacing();
            if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
                DrawProperties();

            ImGui.End();
        }

        // RIGHT SIDEBAR: Entity List (Width reduced to 150px)
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(viewport.Width - 150, 0));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(150, viewport.Height));
        if (
            ImGui.Begin(
                "Entities",
                ImGuiWindowFlags.NoMove
                    | ImGuiWindowFlags.NoResize
                    | ImGuiWindowFlags.NoCollapse
                    | ImGuiWindowFlags.NoSavedSettings
            )
        )
        {
            DrawEntityList();
            ImGui.End();
        }

        DrawResizeModal();
    }

    private void DrawHeader()
    {
        ImGui.TextUnformatted(
            string.IsNullOrWhiteSpace(_levelPath) ? "NEW LEVEL" : Path.GetFileName(_levelPath)
        );

        string saveLabel = _isDirty ? "SAVE*" : "SAVE";
        if (ImGui.Button(saveLabel, new System.Numerics.Vector2(130, 30)))
            SaveLevel();

        ImGui.SameLine();
        if (ImGui.Button("EXIT", new System.Numerics.Vector2(130, 30)))
            _engine.LoadMenu();
    }

    private void DrawEntityList()
    {
        if (ImGui.Button("ADD ENTITY", new System.Numerics.Vector2(-1, 30)))
        {
            var newEnt = new Wall(_level);
            _level.AddEntity(newEnt);
            _selectedEntity = newEnt;
            _isDirty = true;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.BeginChild("List", new System.Numerics.Vector2(-1, 0), ImGuiChildFlags.None))
        {
            for (int i = 0; i < _level.Entities.Count; i++)
            {
                var ent = _level.Entities[i];
                string label = $"[{i}] {GetEntityName(ent)}";
                if (ImGui.Selectable(label, _selectedEntity == ent))
                    _selectedEntity = ent;
            }
            ImGui.EndChild();
        }
    }

    private string GetEntityName(Entity ent)
    {
        return ent switch
        {
            Wall => "Wall",
            Movable => "Movable",
            Piston => "Piston",
            _ => "Unknown",
        };
    }

    private void DrawProperties()
    {
        if (_selectedEntity == null)
        {
            ImGui.TextDisabled("No entity selected.");
            return;
        }

        var ent = _selectedEntity;

        int typeIdx = GetEntityTypeIndex(ent);
        string[] types = ["Wall", "Movable", "Piston"];
        if (ImGui.Combo("Entity Type", ref typeIdx, types, types.Length))
        {
            ConvertSelectedEntity(typeIdx);
            _isDirty = true;
            return;
        }

        ImGui.Separator();
        ImGui.Spacing();

        if (ent is ClusterEntity ce)
        {
            int cId = ce.ClusterId;
            // Draw a color preview box before or after the slider
            var color = RenderUtil.GetClusterColor(cId);
            var imguiColor = new System.Numerics.Vector4(
                color.R / 255f,
                color.G / 255f,
                color.B / 255f,
                1.0f
            );

            ImGui.ColorButton(
                "##ClusterPreview",
                imguiColor,
                ImGuiColorEditFlags.NoTooltip,
                new System.Numerics.Vector2(20, 20)
            );
            ImGui.SameLine();

            if (ImGui.SliderInt("Cluster ID", ref cId, 1, 5))
            {
                ce.ClusterId = cId;
                _isDirty = true;
            }
        }

        if (ent is Piston p)
        {
            int axisIdx = (int)p.Axis;
            string[] axes = ["Horizontal", "Vertical"];
            if (ImGui.Combo("Axis", ref axisIdx, axes, axes.Length))
            {
                p.Axis = (Piston.Orientation)axisIdx;
                _isDirty = true;
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        if (ImGui.Button("DELETE ENTITY", new System.Numerics.Vector2(-1, 30)))
        {
            _level.RemoveEntity(ent);
            _selectedEntity = null;
            _isDirty = true;
        }
    }

    private int GetEntityTypeIndex(Entity ent)
    {
        return ent switch
        {
            Wall => 0,
            Movable => 1,
            Piston => 2,
            _ => 0,
        };
    }

    private void ConvertSelectedEntity(int typeIdx)
    {
        if (_selectedEntity == null)
            return;

        var oldEnt = _selectedEntity;
        var cells = oldEnt.OccupiedCells.ToList();

        Entity newEnt = typeIdx switch
        {
            0 => new Wall(_level),
            1 => new Movable(_level),
            2 => new Piston(_level),
            _ => new Wall(_level),
        };

        _level.RemoveEntity(oldEnt);
        _level.AddEntity(newEnt);
        newEnt.ReplaceCells(cells);

        _selectedEntity = newEnt;
    }

    private void DrawLevelSettings()
    {
        ImGui.InputInt("W", ref _newWidth);
        ImGui.InputInt("H", ref _newHeight);
        _newWidth = Math.Clamp(_newWidth, 5, 100);
        _newHeight = Math.Clamp(_newHeight, 5, 100);

        if (ImGui.Button("RESIZE LEVEL", new System.Numerics.Vector2(-1, 30)))
        {
            if (_level.Entities.Count > 0)
                _showResizeWarning = true;
            else
                PerformResize();
        }
    }

    private void DrawResizeModal()
    {
        if (_showResizeWarning)
            ImGui.OpenPopup("Confirm Resize");
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
                "WARNING: THIS WILL CLEAR ALL ENTITIES!"
            );
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
        _level = new Level(_newWidth, _newHeight, _level.MergeRules, _level.WinRules);
        _gridView.UpdateLevel(_level);
        _selectedEntity = null;
        _lastPaintCell = null;
        _isDirty = true;
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
        foreach (var entity in _level.Entities)
        {
            Color baseColor = GetEntityBaseColor(entity);
            if (entity == _selectedEntity)
                baseColor = RenderUtil.GetHighlightedColor(baseColor);
            _gridView.DrawEntity(entity.OccupiedCells, baseColor);
        }
    }

    private Color GetEntityBaseColor(Entity ent)
    {
        if (ent is Wall)
            return new Color(60, 60, 60);
        if (ent is ClusterEntity ce)
            return RenderUtil.GetClusterColor(ce.ClusterId);
        if (ent is Piston)
            return RenderUtil.GetClusterColor(100);
        return Color.Gray;
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
        _isDirty = false;
    }

    private bool IsKeyPressed(KeyboardState current, Keys key) =>
        current.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);
}
