using Yugo.Core.Game;

namespace Yugo.Core.Serialization;

public sealed record LevelState(
    Direction Gravity,
    List<Snapshot> MergeRules,
    List<Snapshot> WinRules,
    List<Snapshot> Entities
);
