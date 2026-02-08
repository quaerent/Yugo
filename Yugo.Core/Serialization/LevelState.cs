using Yugo.Core.Entities;
using Yugo.Core.Game;
using Yugo.Core.Rules;

namespace Yugo.Core.Serialization;

public sealed record LevelState(
    Direction Gravity,
    List<Snapshot> MergeRules,
    List<Snapshot> WinRules,
    List<Snapshot> Entities
)
{
    public static LevelState Capture(Level level)
    {
        return new LevelState(
            level.Gravity,
            level.MergeRules.Select(SnapshotFactory.Snapshot).ToList(),
            level.WinRules.Select(SnapshotFactory.Snapshot).ToList(),
            level.Entities.Select(SnapshotFactory.Snapshot).ToList()
        );
    }

    public bool Equals(LevelState? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return Gravity == other.Gravity
            && CompareSnapshotLists(MergeRules, other.MergeRules)
            && CompareSnapshotLists(WinRules, other.WinRules)
            && CompareSnapshotLists(Entities, other.Entities);
    }

    private static bool CompareSnapshotLists(List<Snapshot> a, List<Snapshot> b)
    {
        if (a.Count != b.Count)
            return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i].TypeId != b[i].TypeId)
                return false;
            if (a[i].Data.Count != b[i].Data.Count)
                return false;
            foreach (var (key, value) in a[i].Data)
            {
                if (!b[i].Data.TryGetValue(key, out var otherValue) || value != otherValue)
                    return false;
            }
        }
        return true;
    }

    public override int GetHashCode()
    {
        // Simplified HashCode for records when overriding Equals
        var hash = new HashCode();
        hash.Add(Gravity);
        foreach (var s in Entities)
            hash.Add(s.TypeId);
        return hash.ToHashCode();
    }

    public void Apply(Level level)
    {
        foreach (var point in level.Grid.Points())
        {
            level.Grid[point] = null;
        }

        level.Entities.Clear();
        level.Gravity = Gravity;

        level.MergeRules = MergeRules.Select(s => SnapshotFactory.Create<IMergeRule>(s)).ToList();
        level.WinRules = WinRules.Select(s => SnapshotFactory.Create<IWinRule>(s)).ToList();

        foreach (var snapshot in Entities)
        {
            var entity = SnapshotFactory.Create<Entity>(snapshot, level);
            level.Entities.Add(entity);
        }
    }
}
