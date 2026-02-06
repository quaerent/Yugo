using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Yugo.Core.Entities;
using Yugo.Core.Game;
using Yugo.Core.Serialization;
using Yugo.Game.Renderer;

namespace Yugo.Game;

public sealed class Scene
{
    private const int MinHalfUnitPixels = 6;
    private const int MarginPixels = 16;

    private readonly Engine _engine;
    private Level _level;
    private MouseState _previousMouse;
    private Point? _dragStartCell;
    private Point _dragStartScreen;
    private bool _dragConsumed;
    private Entity? _dragEntity;
    private EndState _endState = EndState.None;
    private GridMetrics _gridMetrics;
    private readonly string _levelPath;

    public Scene(Engine engine, string levelPath)
    {
        _engine = engine;
        _levelPath = levelPath;
        _level = LevelXml.Load(levelPath);
        _level.Start();
        UpdateEndState();

        Renderers = new List<IRenderer>
        {
            new GridRenderer(this),
            new WallRenderer(this),
            new MovableRenderer(this),
        };
    }

    public IReadOnlyList<IRenderer> Renderers { get; }
    public Level Level => _level;
    public GridMetrics CurrentGridMetrics => _gridMetrics;
    public Engine Engine => _engine;
    public EndState CurrentEndState => _endState;

    public void Update(GameTime gameTime)
    {
        if (_endState == EndState.None)
            HandleInput();
    }

    public void Render()
    {
        _gridMetrics = GetGridMetrics();
        foreach (var renderer in Renderers)
        {
            renderer.Render();
        }
    }

    public void DrawRect(Rectangle rect, Color color)
    {
        _engine.SpriteBatch.Draw(_engine.Pixel, rect, color);
    }

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

    public Rectangle CellToRect(Point cell)
    {
        var origin = _gridMetrics.Origin;
        var halfUnitPixels = _gridMetrics.HalfUnitPixels;
        var x = origin.X + cell.X * halfUnitPixels;
        var y = origin.Y - (cell.Y + 1) * halfUnitPixels;
        return new Rectangle(x, y, halfUnitPixels, halfUnitPixels);
    }

    public void DrawEntity(IEnumerable<Point> cells, Color color)
    {
        var cellSet = cells as HashSet<Point> ?? [.. cells];
        foreach (var cell in cellSet)
        {
            var rect = CellToRect(cell);

            var insetLeft = cellSet.Contains(new Point(cell.X - 1, cell.Y)) ? 0 : 1;
            var insetUp = cellSet.Contains(new Point(cell.X, cell.Y + 1)) ? 0 : 1;

            var x = rect.X + insetLeft;
            var y = rect.Y + insetUp;
            var width = rect.Width - insetLeft;
            var height = rect.Height - insetUp;

            if (width <= 0 || height <= 0)
                continue;

            DrawRect(new Rectangle(x, y, width, height), color);
        }
    }

    public Point? ScreenToCell(Point screen)
    {
        var origin = _gridMetrics.Origin;
        var gridWidth = _gridMetrics.GridWidth;
        var gridHeight = _gridMetrics.GridHeight;
        var halfUnitPixels = _gridMetrics.HalfUnitPixels;

        if (screen.X < origin.X || screen.X >= origin.X + gridWidth)
            return null;
        if (screen.Y > origin.Y || screen.Y <= origin.Y - gridHeight)
            return null;

        var x = (screen.X - origin.X) / halfUnitPixels;
        var y = (origin.Y - screen.Y - 1) / halfUnitPixels;

        var cell = new Point(x, y);
        return _level.Grid.IsInside(cell) ? cell : null;
    }

    private void HandleInput()
    {
        var mouse = Mouse.GetState();

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
            var threshold = _gridMetrics.HalfUnitPixels * 2;

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
    }

    private GridMetrics GetGridMetrics()
    {
        var viewport = _engine.GraphicsDevice.Viewport;
        var availableWidth = Math.Max(1, viewport.Width - MarginPixels * 2);
        var availableHeight = Math.Max(1, viewport.Height - MarginPixels * 2);

        var halfUnitPixels = Math.Max(
            MinHalfUnitPixels,
            Math.Min(availableWidth / _level.Grid.Width, availableHeight / _level.Grid.Height)
        );

        var gridWidth = _level.Grid.Width * halfUnitPixels;
        var gridHeight = _level.Grid.Height * halfUnitPixels;

        var origin = new Point(
            (viewport.Width - gridWidth) / 2,
            (viewport.Height + gridHeight) / 2
        );

        return new GridMetrics(origin, halfUnitPixels, gridWidth, gridHeight);
    }

    public void ReloadLevel()
    {
        _level = LevelXml.Load(_levelPath);
        _level.Start();
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
        {
            _endState = EndState.Win;
            return;
        }
    }

    public readonly record struct GridMetrics(
        Point Origin,
        int HalfUnitPixels,
        int GridWidth,
        int GridHeight
    );

    public enum EndState
    {
        None,
        Win,
    }
}
