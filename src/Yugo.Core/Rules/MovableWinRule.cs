using Yugo.Core.Entities;
using Yugo.Core.Game;

namespace Yugo.Core.Rules;

public class MovableWinRule : IWinRule
{
    public WinRuleResult IsSatisfied(Level level)
    {
        var appearedClusterIds = new HashSet<int>();
        foreach (var entity in level.Entities.OfType<Movable>())
        {
            if (appearedClusterIds.Contains(entity.ClusterId))
                return WinRuleResult.NotSatisfied;
            appearedClusterIds.Add(entity.ClusterId);
        }
        return WinRuleResult.Satisfied;
    }
}
