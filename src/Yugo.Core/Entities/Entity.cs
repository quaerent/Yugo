using Microsoft.Xna.Framework;
using Yugo.Core.Game;

namespace Yugo.Core.Entities;

/// <summary>
/// Base class for all entities in the game.
/// </summary>
public abstract class Entity
{
    public readonly Level Level;
    private List<Point> _occupiedCells = [];
    public IReadOnlyList<Point> OccupiedCells => _occupiedCells;
    internal List<Point> OccupiedCellsInternal => _occupiedCells;

    protected Grid Grid
    {
        get => Level.Grid;
    }

    public Entity(Level level, IEnumerable<Point> occupiedCells)
    {
        Level = level;
        UpdateOccupiedCells(occupiedCells);
    }

    /// <summary>
    /// Updates the occupied cells of the entity.
    /// </summary>
    /// <param name="newCells">The new set of cells to be occupied by the entity.</param>
    /// <exception cref="InvalidOperationException"></exception>
    protected void UpdateOccupiedCells(IEnumerable<Point> newCells)
    {
        foreach (var cell in _occupiedCells)
        {
            if (!Grid.IsInside(cell))
                continue;
            Grid[cell] = null;
        }
        foreach (var cell in newCells)
        {
            if (!Grid.IsInside(cell))
                continue;
            if (Grid[cell] != null)
                throw new InvalidOperationException("Cell is already occupied.");
            Grid[cell] = this;
        }
        _occupiedCells = [.. newCells];
    }

    protected void Translate(Point offset)
    {
        var newCells = OccupiedCells.Select(p => p + offset);
        UpdateOccupiedCells(newCells);
    }

    /// <summary>
    /// Gets a list of entities obstructing movement in the given direction.
    /// </summary>
    /// <param name="dir">The direction in which to check for obstacles.</param>
    /// <returns>A list of entities that obstruct movement in the given direction.</returns>
    protected HashSet<Entity> GetObstacles(Direction dir)
    {
        var offset = dir.ToPoint();
        var obstacles = new HashSet<Entity>();
        foreach (var cell in OccupiedCells)
        {
            var target = new Point(cell.X + offset.X, cell.Y + offset.Y);
            if (!Grid.IsInside(target))
                continue;
            var occupant = Grid[target];
            if (occupant != null && occupant != this && !obstacles.Contains(occupant))
                obstacles.Add(occupant);
        }
        return obstacles;
    }

    /// <summary>
    /// Calculates the maximum number of free moves the entity can make in the given direction.
    /// </summary>
    /// <param name="dir">The direction in which to calculate the maximum free move.</param>
    /// <returns>The maximum number of free moves the entity can make in the given direction.</returns>
    protected int MaxFreeMove(Direction dir)
    {
        var offset = dir.ToPoint();
        var maxMove = int.MaxValue;
        foreach (var cell in OccupiedCells)
        {
            var move = 0;
            while (true)
            {
                var target = new Point(
                    cell.X + offset.X * (move + 1),
                    cell.Y + offset.Y * (move + 1)
                );
                if (!Grid.IsInside(target))
                {
                    move = int.MaxValue;
                    break;
                }
                var occupant = Grid[target];
                if (occupant == this)
                {
                    move = int.MaxValue;
                    break;
                }
                else if (occupant == null)
                    move++;
                else
                    break;
            }
            if (move < maxMove)
                maxMove = move;
        }
        if (maxMove == int.MaxValue)
            // make sure that the entity can move out of the grid
            return Grid.Width + Grid.Height;
        return maxMove;
    }

    /// <summary>
    /// Called when the entity is clicked.
    /// </summary>
    public abstract void OnClick();

    /// <summary>
    /// Called when the entity is moved in the given direction.
    /// </summary>
    /// <param name="dir">The direction in which the entity is moved.</param>
    public abstract void OnMove(Direction dir);

    /// <summary>
    /// Called when the entity is pushed in the given direction.
    /// </summary>
    /// <param name="dir">The direction in which the entity is pushed.</param>
    /// <returns>A collection of points representing the new positions after the push.</returns>
    public abstract IEnumerable<Point>? TryPush(Direction dir);

    /// <summary>
    /// Called when the entity has been pushed in the given direction.
    /// The occupied cells have already been updated when this method is called.
    /// </summary>
    /// <param name="dir">The direction in which the entity is pushed.</param>
    /// <param name="newPositions">A collection of points representing the new positions after the push.</param>
    public abstract void OnPush(Direction dir);

    /// <summary>
    /// Determines whether the entity needs to be updated (e.g., due to the gravity).
    /// </summary>
    /// <returns>True if the entity needs to be updated, otherwise false.</returns>
    public abstract bool NeedsUpdate();

    /// <summary>
    /// Updates the entity's state.
    /// </summary>
    public abstract void Update();
}
