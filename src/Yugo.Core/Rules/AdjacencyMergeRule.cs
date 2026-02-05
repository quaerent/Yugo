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
    /// <param name="a">The first entity to check.</param>
    /// <param name="b">The second entity to check.</param>
    /// <returns>True if the two entities are mergeable according to this rule; otherwise, false.</returns>
    protected abstract bool IsMergeable(EntityType a, EntityType b);

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

            var rightEntity = GetEntity(level.Grid, new Point(point.X + 1, point.Y));
            var downEntity = GetEntity(level.Grid, new Point(point.X, point.Y + 1));
            if (rightEntity != null && rightEntity != entity && IsMergeable(entity, rightEntity))
                graph.AddEdge(entity, rightEntity);
            if (downEntity != null && downEntity != entity && IsMergeable(entity, downEntity))
                graph.AddEdge(entity, downEntity);
        }

        var entitiesToRemove = new HashSet<Entity>();
        foreach (var entity in level.Entities.OfType<EntityType>())
        {
            var root = graph.Find(entity);
            if (root != entity)
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

    private class AdjacencyGraph<T>
        where T : notnull
    {
        private Dictionary<T, T> _parents = new();

        public T Find(T item)
        {
            var parent = _parents.GetValueOrDefault(item, item);
            if (!parent.Equals(item))
            {
                parent = Find(parent);
                _parents[item] = parent;
            }
            return parent;
        }

        public void AddEdge(T a, T b)
        {
            var rootA = Find(a);
            var rootB = Find(b);
            if (!rootA.Equals(rootB))
            {
                _parents[rootB] = rootA;
            }
        }
    }
}
