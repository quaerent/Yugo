using Yugo.Core.Game;
using Yugo.Core.Serialization;

namespace Yugo.Core.Rules;

public interface IWinRule : ISnapshotSerializable
{
    /// <summary>
    /// Determines whether the win rule is satisfied for the specified level.
    /// </summary>
    /// <param name="level">The level to check.</param>
    /// <returns>A WinRuleResult indicating whether the win rule is satisfied.</returns>
    WinRuleResult IsSatisfied(Level level);
}

public enum WinRuleResult
{
    /// <summary>
    /// The win rule is satisfied.
    /// </summary>
    Satisfied,

    /// <summary>
    /// The win rule is not satisfied.
    /// </summary>
    NotSatisfied,

    /// <summary>
    /// The level is completed even if other win rules are not satisfied.
    /// </summary>
    AllSatisfied,
}
