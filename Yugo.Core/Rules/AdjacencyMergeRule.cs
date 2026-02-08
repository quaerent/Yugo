using Microsoft.Xna.Framework;
using Yugo.Core.Entities;
using Yugo.Core.Game;

namespace Yugo.Core.Rules;

public abstract class AdjacencyMergeRule<EntityType> : IMergeRule
    where EntityType : Entity
{
    /// <summary>
    /// Determines whether two entities are mergeable according to this rule.
    /// </summary>
    protected abstract bool IsMergeable(EntityType a, EntityType b);

    /// <summary>
    /// Determines the priority of an entity during merging.
    /// Higher priority entities will become the root of the merge.
    /// </summary>
    protected virtual int GetPriority(EntityType entity) => 0;

    private EntityType? GetEntity(Grid grid, Point point)
    {
        if (!grid.IsInside(point))
            return null;
        return grid[point] as EntityType;
    }

    void IMergeRule.Apply(Level level)
    {
        var graph = new AdjacencyGraph<EntityType>();

        foreach (var point in level.Grid.Points())
        {
            var entity = GetEntity(level.Grid, point);
            if (entity == null)
                continue;

            var neighbors = new[]
            {
                new Point(point.X + 1, point.Y),
                new Point(point.X, point.Y + 1),
            };

            foreach (var neighborPos in neighbors)
            {
                var neighbor = GetEntity(level.Grid, neighborPos);
                if (neighbor != null && neighbor != entity && IsMergeable(entity, neighbor))
                {
                    graph.AddEdge(entity, neighbor, GetPriority);
                }
            }
        }

        var entitiesToRemove = new HashSet<Entity>();
        foreach (var entity in level.Entities.OfType<EntityType>().Where(e => !e.Dead))
        {
            var root = graph.Find(entity);
            if (!ReferenceEquals(root, entity))
            {
                entitiesToRemove.Add(entity);
                root.OccupiedCellsInternal.AddRange(entity.OccupiedCells);
                foreach (var cell in entity.OccupiedCells)
                {
                    level.Grid[cell] = root;
                }
            }
        }
        level.Entities.RemoveAll(entitiesToRemove.Contains);
    }

    public void Serialize(Dictionary<string, string> data) { }

    public void Deserialize(Dictionary<string, string> data) { }

    private class AdjacencyGraph<T>
        where T : notnull
    {
        private Dictionary<T, T> _parents = new();

        public T Find(T item)
        {
            if (!_parents.TryGetValue(item, out var parent) || ReferenceEquals(parent, item))
                return item;

            parent = Find(parent);
            _parents[item] = parent;
            return parent;
        }

        public void AddEdge(T a, T b, Func<T, int> priorityFunc)
        {
            var rootA = Find(a);
            var rootB = Find(b);
            if (!ReferenceEquals(rootA, rootB))
            {
                if (priorityFunc(rootB) > priorityFunc(rootA))
                    _parents[rootA] = rootB;
                else
                    _parents[rootB] = rootA;
            }
        }
    }
}
