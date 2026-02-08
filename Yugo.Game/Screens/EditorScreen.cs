using System.Text.Json;
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
    private LevelIdentity _identity;
    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;
    private Point? _lastPaintCell;

    private Entity? _selectedEntity;
    private int _newWidth = 30;
    private int _newHeight = 18;
    private bool _showResizeWarning = false;
    private bool _isDirty = false;
    private bool _isSyncing = false;

    private bool IsReadOnly => _identity.IsCloud && _identity.AuthorId != _engine.CurrentUser?.Id;

    public EditorScreen(Engine engine, Level level, LevelIdentity identity)
    {
        _engine = engine;
        _level = level;
        _identity = identity;
        _gridView = new GridView(engine, level);
        _newWidth = level.Grid.Width;
        _newHeight = level.Grid.Height;
    }

    public void ConfirmSync(int newCloudId)
    {
        _identity = _identity with { CloudId = newCloudId, AuthorId = _engine.CurrentUser?.Id };
        _isDirty = false;
    }

    public void Update(GameTime gameTime)
    {
        if (IsReadOnly)
        {
            if (IsKeyPressed(Keyboard.GetState(), Keys.Escape))
                _engine.LoadMenu();
            return;
        }

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
                _selectedEntity = cell.HasValue ? _level.Grid[cell.Value] : null;
            }
            else
                _lastPaintCell = null;
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
            entity.ReplaceCells(remaining);
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
                ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse
            )
        )
        {
            DrawHeader();
            ImGui.Separator();

            if (IsReadOnly)
            {
                ImGui.TextColored(new System.Numerics.Vector4(1, 0.5f, 0, 1), "READ-ONLY MODE");
                ImGui.TextWrapped("You are not the author of this cloud level.");
            }
            else
            {
                if (ImGui.CollapsingHeader("Level Settings", ImGuiTreeNodeFlags.DefaultOpen))
                    DrawLevelSettings();
                ImGui.Spacing();
                if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
                    DrawProperties();
                ImGui.Spacing();
                if (ImGui.CollapsingHeader("Cloud Sync"))
                    DrawCloudSync();
            }
            ImGui.End();
        }

        ImGui.SetNextWindowPos(new System.Numerics.Vector2(viewport.Width - 150, 0));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(150, viewport.Height));
        if (
            ImGui.Begin(
                "Entities",
                ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse
            )
        )
        {
            if (ImGui.BeginChild("List", new System.Numerics.Vector2(-1, 0)))
            {
                if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    _selectedEntity = null;
                if (!IsReadOnly)
                    DrawEntityList();
                else
                    ImGui.TextDisabled("Editing disabled");
                ImGui.EndChild();
            }
            ImGui.End();
        }
        DrawResizeModal();
    }

    private void DrawHeader()
    {
        ImGui.TextUnformatted(
            _identity.IsCloud ? $"CLOUD: {_identity.Title}" : $"LOCAL: {_identity.Title}"
        );
        if (!IsReadOnly)
        {
            if (ImGui.Button(_isDirty ? "SAVE*" : "SAVE", new System.Numerics.Vector2(130, 30)))
                SaveLevel();
            ImGui.SameLine();
        }
        if (ImGui.Button("EXIT", new System.Numerics.Vector2(130, 30)))
            _engine.LoadMenu();
    }

    private void DrawEntityList()
    {
        if (ImGui.Button("ADD ENTITY", new System.Numerics.Vector2(-1, 30)))
        {
            Entity newEnt = _selectedEntity switch
            {
                Piston p => new Piston(_level) { ClusterId = p.ClusterId, Axis = p.Axis },
                Movable m => new Movable(_level) { ClusterId = m.ClusterId },
                _ => new Wall(_level),
            };
            _level.AddEntity(newEnt);
            _selectedEntity = newEnt;
            _isDirty = true;
        }
        ImGui.Spacing();
        ImGui.Separator();
        for (int i = 0; i < _level.Entities.Count; i++)
        {
            var ent = _level.Entities[i];
            if (ImGui.Selectable($"[{i}] {GetEntityName(ent)}", _selectedEntity == ent))
                _selectedEntity = ent;
        }
    }

    private string GetEntityName(Entity ent) =>
        ent switch
        {
            Wall => "Wall",
            Movable => "Movable",
            Piston => "Piston",
            _ => "Unknown",
        };

    private void DrawProperties()
    {
        if (_selectedEntity == null)
        {
            ImGui.TextDisabled("No entity selected.");
            return;
        }
        var ent = _selectedEntity;
        int typeIdx = ent switch
        {
            Wall => 0,
            Movable => 1,
            Piston => 2,
            _ => 0,
        };
        if (ImGui.Combo("Entity Type", ref typeIdx, ["Wall", "Movable", "Piston"], 3))
        {
            ConvertSelectedEntity(typeIdx);
            return;
        }
        ImGui.Separator();
        if (ent is ClusterEntity ce)
        {
            int cId = ce.ClusterId;
            var color = RenderUtil.GetClusterColor(cId);
            ImGui.ColorButton(
                "##CP",
                new System.Numerics.Vector4(color.R / 255f, color.G / 255f, color.B / 255f, 1),
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
            if (ImGui.Combo("Axis", ref axisIdx, ["Horizontal", "Vertical"], 2))
            {
                p.Axis = (Piston.Orientation)axisIdx;
                _isDirty = true;
            }
        }
        if (ImGui.Button("DELETE ENTITY", new System.Numerics.Vector2(-1, 30)))
        {
            _level.RemoveEntity(ent);
            _selectedEntity = null;
            _isDirty = true;
        }
    }

    private void DrawCloudSync()
    {
        if (_engine.CurrentUser == null)
        {
            ImGui.TextDisabled("Login required.");
            return;
        }
        if (_identity.IsCloud)
        {
            ImGui.TextColored(
                new System.Numerics.Vector4(0, 0.8f, 0, 1),
                $"Linked to Cloud ID: {_identity.CloudId}"
            );
            if (
                ImGui.Button(
                    _isSyncing ? "SAVING..." : "UPDATE ON CLOUD",
                    new System.Numerics.Vector2(-1, 30)
                ) && !_isSyncing
            )
                SyncToCloud();
        }
        else
        {
            if (
                ImGui.Button(
                    _isSyncing ? "SYNCING..." : "SHARE TO CLOUD",
                    new System.Numerics.Vector2(-1, 30)
                ) && !_isSyncing
            )
                SyncToCloud();
        }
    }

    private void SyncToCloud()
    {
        if (_engine.CurrentUser == null)
            return;
        _isSyncing = true;
        try
        {
            var xml = LevelXml.ToXElement(_level).ToString();
            _engine.RemoteService.SendMessage(
                JsonSerializer.Serialize(
                    new
                    {
                        command = "share_level",
                        title = _identity.Title,
                        xmlData = xml,
                        cloudId = _identity.CloudId ?? -1,
                    }
                )
            );
        }
        catch { }
        finally
        {
            _isSyncing = false;
        }
    }

    private void SaveLevel()
    {
        if (_identity.IsCloud)
        {
            SyncToCloud();
            return;
        }
        var path = string.IsNullOrEmpty(_identity.LocalPath)
            ? FileDialog.SaveLevelFile()
            : _identity.LocalPath;
        if (string.IsNullOrEmpty(path))
            return;
        if (!path.EndsWith(".xml"))
            path += ".xml";
        LevelXml.Save(_level, path);
        _identity = _identity with { LocalPath = path };
        _engine.RegisterRecentLevel(_identity);
        _isDirty = false;
    }

    private void ConvertSelectedEntity(int typeIdx)
    {
        if (_selectedEntity == null)
            return;
        var cells = _selectedEntity.OccupiedCells.ToList();
        Entity newEnt = typeIdx switch
        {
            1 => new Movable(_level),
            2 => new Piston(_level),
            _ => new Wall(_level),
        };
        _level.RemoveEntity(_selectedEntity);
        _level.AddEntity(newEnt);
        newEnt.ReplaceCells(cells);
        _selectedEntity = newEnt;
        _isDirty = true;
    }

    private void DrawLevelSettings()
    {
        ImGui.InputInt("W", ref _newWidth);
        ImGui.InputInt("H", ref _newHeight);
        _newWidth = Math.Clamp(_newWidth, 5, 100);
        _newHeight = Math.Clamp(_newHeight, 5, 100);
        if (ImGui.Button("RESIZE", new System.Numerics.Vector2(-1, 30)))
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
            ImGui.OpenPopup("Resize");
        if (
            ImGui.BeginPopupModal(
                "Resize",
                ref _showResizeWarning,
                ImGuiWindowFlags.AlwaysAutoResize
            )
        )
        {
            ImGui.Text("CLEAR ALL ENTITIES?");
            if (ImGui.Button("YES", new System.Numerics.Vector2(120, 0)))
            {
                PerformResize();
                _showResizeWarning = false;
            }
            ImGui.SameLine();
            if (ImGui.Button("NO", new System.Numerics.Vector2(120, 0)))
                _showResizeWarning = false;
            ImGui.EndPopup();
        }
    }

    private void PerformResize()
    {
        _level = new Level(_newWidth, _newHeight, _level.MergeRules, _level.WinRules);
        _gridView.UpdateLevel(_level);
        _selectedEntity = null;
        _isDirty = true;
    }

    private void DrawGridLines()
    {
        var m = _gridView.Metrics;
        for (int x = 0; x <= _level.Grid.Width; x += 2)
            _gridView.DrawRect(
                new Rectangle(
                    m.Origin.X + x * m.HalfUnitPixels,
                    m.Origin.Y - m.GridHeight,
                    1,
                    m.GridHeight
                ),
                Color.Black * 0.2f
            );
        for (int y = 0; y <= _level.Grid.Height; y += 2)
            _gridView.DrawRect(
                new Rectangle(m.Origin.X, m.Origin.Y - y * m.HalfUnitPixels, m.GridWidth, 1),
                Color.Black * 0.2f
            );
    }

    private void DrawEntities()
    {
        foreach (var e in _level.Entities)
        {
            Color c = e switch
            {
                Wall => new Color(60, 60, 60),
                Movable => RenderUtil.GetClusterColor(((Movable)e).ClusterId),
                Piston => RenderUtil.GetClusterColor(100),
                _ => Color.Gray,
            };
            if (e == _selectedEntity)
                c = RenderUtil.GetHighlightedColor(c);
            _gridView.DrawEntity(e.OccupiedCells, c);
        }
    }

    private bool IsKeyPressed(KeyboardState c, Keys k) =>
        c.IsKeyDown(k) && !_previousKeyboard.IsKeyDown(k);
}
