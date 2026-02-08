using Microsoft.Xna.Framework;
using Yugo.Core.Entities;
using Yugo.Core.Rules;
using Yugo.Core.Serialization;

namespace Yugo.Core.Game;

public class Level(
    int width,
    int height,
    IEnumerable<IMergeRule> mergeRules,
    IEnumerable<IWinRule> winRules
)
{
    public readonly Grid Grid = new(width, height);
    public List<Entity> Entities = [];
    public List<IMergeRule> MergeRules = [.. mergeRules];
    public List<IWinRule> WinRules = [.. winRules];
    public Direction Gravity = Direction.Down;
    private readonly Stack<LevelState> _history = new();
    private LevelState? _initialState;

    const int MaxIterations = 100;

    public void AddEntity(Entity entity)
    {
        if (!ReferenceEquals(entity.Level, this))
            throw new InvalidOperationException("Entity must be bound to this level instance.");

        Entities.Add(entity);
    }

    public void RemoveEntity(Entity entity)
    {
        if (!Entities.Contains(entity))
            return;

        entity.ReplaceCells([]);
        Entities.Remove(entity);
    }

    public void TryPush(Entity entity, Direction dir)
    {
        if (entity.Dead)
            return;
        var testGrid = new Grid(Grid.Width, Grid.Height);
        var pushQueue = new Queue<Entity>([entity]);
        var pushed = new HashSet<Entity>();
        var success = true;

        while (pushQueue.Count > 0)
        {
            var current = pushQueue.Dequeue();
            if (current.Dead)
            {
                success = false;
                break;
            }
            if (pushed.Contains(current))
                continue;

            var newCells = current.TryPush(dir);
            if (newCells == null)
            {
                // cannot push current entity
                success = false;
                break;
            }

            foreach (var cell in newCells)
            {
                if (!Grid.IsInside(cell))
                    continue;

                // check for occupant in the test grid first
                if (testGrid[cell] != null)
                {
                    // overlap with another pushed entity
                    success = false;
                    break;
                }

                // check for occupant in the actual grid
                var occupant = Grid[cell];
                if (occupant != null && occupant.Dead)
                    continue;
                if (occupant != null && occupant != current && !pushed.Contains(occupant))
                {
                    // need to push the occupant as well
                    pushQueue.Enqueue(occupant);
                }

                testGrid[cell] = current;
            }

            if (!success)
                break;

            pushed.Add(current);
        }

        if (success)
        {
            // remove old cells for pushed entities
            var oldCells = pushed.ToDictionary(e => e, e => e.OccupiedCells.ToList());
            foreach (var (pushedEntity, cells) in oldCells)
            {
                foreach (var cell in cells)
                {
                    if (Grid.IsInside(cell) && ReferenceEquals(Grid[cell], pushedEntity))
                        Grid[cell] = null;
                }
                pushedEntity.OccupiedCellsInternal.Clear();
            }

            // apply new cells only for pushed entities
            foreach (var (point, occupant) in testGrid.Values())
            {
                if (occupant == null)
                    continue;
                Grid[point] = occupant;
                occupant.OccupiedCellsInternal.Add(point);
            }

            foreach (var e in pushed)
            {
                e.OnPush(dir);
            }
        }
    }

    /// <summary>
    /// Starts the level simulation.
    /// </summary>
    public void Start()
    {
        RunUntilStable();
        _initialState ??= LevelState.Capture(this);
    }

    /// <summary>
    /// Handles a click on the given entity.
    /// </summary>
    /// <param name="entity">The entity that was clicked.</param>
    public void Click(Entity entity)
    {
        if (entity.Dead)
            return;

        var before = LevelState.Capture(this);
        entity.OnClick();
        RunUntilStable();
        var after = LevelState.Capture(this);

        if (!before.Equals(after))
        {
            _history.Push(before);
        }
    }

    /// <summary>
    /// Moves the entity in the specified direction.
    /// </summary>
    /// <param name="entity">The entity to move.</param>
    /// <param name="dir">The direction to move the entity.</param>
    public void Move(Entity entity, Direction dir)
    {
        if (entity.Dead)
            return;

        var before = LevelState.Capture(this);
        entity.OnMove(dir);
        RunUntilStable();
        var after = LevelState.Capture(this);

        if (!before.Equals(after))
        {
            _history.Push(before);
        }
    }

    public void Undo()
    {
        if (_history.Count == 0)
            return;

        _history.Pop().Apply(this);
    }

    public void Retry()
    {
        if (_initialState == null)
            return;

        var current = LevelState.Capture(this);
        if (!current.Equals(_initialState))
        {
            _history.Push(current);
            _initialState.Apply(this);
        }
    }

    private void RunUntilStable()
    {
        var count = 0;

        while (true)
        {
            if (count++ >= MaxIterations)
                throw new InvalidOperationException("Max iterations reached while updating level.");

            var entitiesToUpdate = Entities.Where(e => !e.Dead && e.NeedsUpdate()).ToList();
            if (entitiesToUpdate.Count == 0)
                break;

            // always update entities from bottom to top (in the direction of gravity)
            // and from left to right (perpendicular to gravity)
            entitiesToUpdate.Sort(
                (a, b) =>
                {
                    var dir = Gravity.ToPoint();
                    var aHeight = a.OccupiedCells.Max(p => p.X * dir.X + p.Y * dir.Y);
                    var bHeight = b.OccupiedCells.Max(p => p.X * dir.X + p.Y * dir.Y);
                    if (aHeight != bHeight)
                        return aHeight - bHeight;
                    var aPos = a.OccupiedCells.Max(p => p.X * dir.Y - p.Y * dir.X);
                    var bPos = b.OccupiedCells.Max(p => p.X * dir.Y - p.Y * dir.X);
                    if (aPos != bPos)
                        return aPos - bPos;
                    return Entities.IndexOf(a) - Entities.IndexOf(b);
                }
            );

            foreach (var e in entitiesToUpdate)
            {
                e.Update();
            }
        }

        // apply merge rules
        foreach (var rule in MergeRules)
        {
            rule.Apply(this);
        }
    }

    public bool IsCompleted()
    {
        if (WinRules.Count == 0)
            return false;

        var allSatisfied = true;
        foreach (var rule in WinRules)
        {
            switch (rule.IsSatisfied(this))
            {
                case WinRuleResult.Satisfied:
                    continue;
                case WinRuleResult.NotSatisfied:
                    allSatisfied = false;
                    continue;
                case WinRuleResult.AllSatisfied:
                    return true;
            }
        }
        return allSatisfied;
    }

    private void PushHistory()
    {
        _history.Push(LevelState.Capture(this));
    }
}
