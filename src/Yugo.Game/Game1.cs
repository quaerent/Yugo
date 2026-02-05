using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Yugo.Core.Entities;
using Yugo.Core.Game;
using Yugo.Core.Serialization;

namespace Yugo.Game;

public class Game1 : Microsoft.Xna.Framework.Game
{
    private const int HalfUnitPixels = 24;
    private const int MarginPixels = 32;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;

    private Level _level = null!;
    private Point? _selectedCell;
    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);

        var levelPath = Path.Combine(AppContext.BaseDirectory, "Content", "Levels", "default.xml");
        _level = LevelXml.Load(levelPath);
        _level.Start();
    }

    protected override void Update(GameTime gameTime)
    {
        if (
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape)
        )
            Exit();

        HandleInput();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        DrawGrid();
        DrawEntities();
        DrawSelection();

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void HandleInput()
    {
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();

        if (
            mouse.LeftButton == ButtonState.Pressed
            && _previousMouse.LeftButton == ButtonState.Released
        )
        {
            var cell = ScreenToCell(mouse.Position);
            _selectedCell = cell;
        }

        if (
            mouse.RightButton == ButtonState.Pressed
            && _previousMouse.RightButton == ButtonState.Released
        )
        {
            var cell = ScreenToCell(mouse.Position);
            if (cell.HasValue)
                _level.Click(cell.Value);
        }

        if (_selectedCell.HasValue)
        {
            if (IsKeyPressed(keyboard, Keys.Left))
                _level.Move(_selectedCell.Value, Direction.Left);
            if (IsKeyPressed(keyboard, Keys.Right))
                _level.Move(_selectedCell.Value, Direction.Right);
        }

        _previousMouse = mouse;
        _previousKeyboard = keyboard;
    }

    private bool IsKeyPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);
    }

    private void DrawGrid()
    {
        var gridWidth = _level.Grid.Width * HalfUnitPixels;
        var gridHeight = _level.Grid.Height * HalfUnitPixels;
        var origin = GetGridOrigin();

        // Draw full-cell grid lines (1 cell = 2 half-units)
        for (int x = 0; x <= _level.Grid.Width; x += 2)
        {
            var xPixel = origin.X + x * HalfUnitPixels;
            DrawRect(
                new Rectangle(xPixel, origin.Y - gridHeight, 1, gridHeight),
                Color.Black * 0.35f
            );
        }

        for (int y = 0; y <= _level.Grid.Height; y += 2)
        {
            var yPixel = origin.Y - y * HalfUnitPixels;
            DrawRect(new Rectangle(origin.X, yPixel, gridWidth, 1), Color.Black * 0.35f);
        }
    }

    private void DrawEntities()
    {
        foreach (var entity in _level.Entities)
        {
            var color = entity switch
            {
                Movable movable => GetClusterColor(movable.ClusterId),
                _ => new Color(50, 50, 50),
            };

            foreach (var cell in entity.OccupiedCells)
            {
                var rect = CellToRect(cell);
                DrawRect(rect, color);
            }
        }
    }

    private void DrawSelection()
    {
        if (!_selectedCell.HasValue)
            return;

        var rect = CellToRect(_selectedCell.Value);
        DrawRect(new Rectangle(rect.X, rect.Y, rect.Width, 2), Color.Yellow);
        DrawRect(new Rectangle(rect.X, rect.Y + rect.Height - 2, rect.Width, 2), Color.Yellow);
        DrawRect(new Rectangle(rect.X, rect.Y, 2, rect.Height), Color.Yellow);
        DrawRect(new Rectangle(rect.X + rect.Width - 2, rect.Y, 2, rect.Height), Color.Yellow);
    }

    private Rectangle CellToRect(Point cell)
    {
        var origin = GetGridOrigin();
        var x = origin.X + cell.X * HalfUnitPixels;
        var y = origin.Y - (cell.Y + 1) * HalfUnitPixels;
        return new Rectangle(x, y, HalfUnitPixels, HalfUnitPixels);
    }

    private Point? ScreenToCell(Point screen)
    {
        var origin = GetGridOrigin();
        var gridWidth = _level.Grid.Width * HalfUnitPixels;
        var gridHeight = _level.Grid.Height * HalfUnitPixels;

        if (screen.X < origin.X || screen.X >= origin.X + gridWidth)
            return null;
        if (screen.Y > origin.Y || screen.Y <= origin.Y - gridHeight)
            return null;

        var x = (screen.X - origin.X) / HalfUnitPixels;
        var y = (origin.Y - screen.Y - 1) / HalfUnitPixels;

        var cell = new Point(x, y);
        return _level.Grid.IsInside(cell) ? cell : null;
    }

    private Point GetGridOrigin()
    {
        var height = _level.Grid.Height * HalfUnitPixels;
        return new Point(MarginPixels, MarginPixels + height);
    }

    private void DrawRect(Rectangle rect, Color color)
    {
        _spriteBatch.Draw(_pixel, rect, color);
    }

    private static Color GetClusterColor(int clusterId)
    {
        return clusterId switch
        {
            1 => new Color(242, 117, 96),
            2 => new Color(122, 206, 255),
            3 => new Color(145, 220, 160),
            4 => new Color(242, 200, 90),
            _ => new Color(200, 200, 200),
        };
    }
}
