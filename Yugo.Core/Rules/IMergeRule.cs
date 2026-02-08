using Yugo.Core.Game;
using Yugo.Core.Serialization;

namespace Yugo.Core.Rules;

public interface IMergeRule : ISnapshotSerializable
{
    /// <summary>
    /// Applies the merge rule to the specified level.
    /// </summary>
    /// <param name="level">The level to which the merge rule is applied.</param>
    void Apply(Level level);
}
