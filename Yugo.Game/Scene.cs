using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Yugo.Core.Entities;
using Yugo.Core.Game;
using Yugo.Core.Serialization;
using Yugo.Game.Renderer;
using Yugo.Game.Renderers;
using Yugo.Game.Screens;

namespace Yugo.Game;

public sealed class Scene : IScreen
{
    private readonly Engine _engine;
    private Level _level;
    private LevelIdentity _identity;
    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;
    private Point? _dragStartCell;
    private Point _dragStartScreen;
    private bool _dragConsumed;
    private Entity? _dragEntity;
    private EndState _endState = EndState.None;
    private readonly List<IRenderer> _renderers;
    private GridView _gridView;

    public Scene(Engine engine, LevelIdentity identity)
    {
        _engine = engine;
        _identity = identity;
        _level = LevelXml.Load(identity.LocalPath);
        _level.Start();
        UpdateEndState();
        _gridView = new GridView(engine, _level);

        _renderers = new List<IRenderer>
        {
            new GridRenderer(this),
            new WallRenderer(this),
            new MovableRenderer(this),
            new PistonRenderer(this),
        };
    }

    public IReadOnlyList<IRenderer> Renderers => _renderers;
    public Level Level => _level;
    public GridMetrics CurrentGridMetrics => _gridView.Metrics;
    public Engine Engine => _engine;
    public EndState CurrentEndState => _endState;

    public Entity? GetHighlightedEntity()
    {
        if (_dragEntity != null)
            return _dragEntity;
        var mouse = Mouse.GetState();
        var cell = ScreenToCell(mouse.Position);
        if (cell.HasValue)
        {
            var ent = _level.Grid[cell.Value];
            if (ent is Movable)
                return ent;
        }
        return null;
    }

    public void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (IsKeyPressed(keyboard, Keys.Escape) && _previousKeyboard.IsKeyUp(Keys.Escape))
        {
            _engine.LoadMenu();
            _previousKeyboard = keyboard;
            return;
        }

        if (_endState == EndState.None)
            HandleInput();
        UpdateEndState();
    }

    public void Render()
    {
        _gridView.Update();
        foreach (var renderer in _renderers)
            renderer.Render();
    }

    public void DrawGui()
    {
        // Add gameplay UI here
    }

    public void DrawRect(Rectangle rect, Color color) => _gridView.DrawRect(rect, color);

    public void DrawLine(Point from, Point to, int thickness, Color color)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var length = MathF.Sqrt(dx * dx + dy * dy);
        if (length <= 0.01f)
            return;

        var rotation = MathF.Atan2(dy, dx);
        var rect = new Rectangle(from.X, from.Y, (int)length, thickness);
        _engine.SpriteBatch.Draw(
            _engine.Pixel,
            rect,
            null,
            color,
            rotation,
            Vector2.Zero,
            SpriteEffects.None,
            0
        );
    }

    public Rectangle CellToRect(Point cell) => _gridView.CellToRect(cell);

    public void DrawEntity(IEnumerable<Point> cells, Color color) =>
        _gridView.DrawEntity(cells, color);

    public Point? ScreenToCell(Point screen) => _gridView.ScreenToCell(screen);

    private void HandleInput()
    {
        if (!_engine.IsActive)
            return;

        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();

        if (IsKeyPressed(keyboard, Keys.Back))
        {
            _level.Undo();
            UpdateEndState();
        }
        else if (IsKeyPressed(keyboard, Keys.R))
        {
            _level.Retry();
            UpdateEndState();
        }

        if (
            mouse.LeftButton == ButtonState.Pressed
            && _previousMouse.LeftButton == ButtonState.Released
        )
        {
            _dragStartCell = ScreenToCell(mouse.Position);
            _dragStartScreen = mouse.Position;
            _dragConsumed = false;
            _dragEntity = _dragStartCell.HasValue ? _level.Grid[_dragStartCell.Value] : null;
        }

        if (
            mouse.LeftButton == ButtonState.Pressed
            && _previousMouse.LeftButton == ButtonState.Pressed
            && _dragEntity != null
        )
        {
            var dx = mouse.X - _dragStartScreen.X;
            var dy = mouse.Y - _dragStartScreen.Y;
            var threshold = _gridView.Metrics.HalfUnitPixels * 2;

            if (Math.Abs(dx) >= threshold || Math.Abs(dy) >= threshold)
            {
                var direction =
                    Math.Abs(dx) >= Math.Abs(dy)
                        ? (dx >= 0 ? Direction.Right : Direction.Left)
                        : (dy <= 0 ? Direction.Up : Direction.Down);

                _level.Move(_dragEntity, direction);
                _dragConsumed = true;
                _dragStartScreen = mouse.Position;
                UpdateEndState();
            }
        }

        if (
            mouse.LeftButton == ButtonState.Released
            && _previousMouse.LeftButton == ButtonState.Pressed
        )
        {
            if (_dragEntity != null && !_dragConsumed)
            {
                _level.Click(_dragEntity);
                UpdateEndState();
            }
            _dragStartCell = null;
            _dragEntity = null;
        }

        _previousMouse = mouse;
        _previousKeyboard = keyboard;
    }

    private bool IsKeyPressed(KeyboardState current, Keys key) =>
        current.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);

    public void ReloadLevel()
    {
        _level = LevelXml.Load(_identity.LocalPath);
        _level.Start();
        _gridView = new GridView(_engine, _level);
        _dragStartCell = null;
        _dragConsumed = false;
        _dragEntity = null;
        _endState = EndState.None;
        UpdateEndState();
    }

    private void UpdateEndState()
    {
        if (_endState != EndState.None)
            return;
        if (_level.IsCompleted())
            _endState = EndState.Win;
    }

    public enum EndState
    {
        None,
        Win,
    }
}
