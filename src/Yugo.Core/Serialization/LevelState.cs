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
