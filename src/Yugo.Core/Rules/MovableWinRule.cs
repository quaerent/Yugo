using Yugo.Core.Entities;
using Yugo.Core.Game;
using Yugo.Core.Serialization;

namespace Yugo.Core.Rules;

[TypeId("win-movable", "Movable Win")]
public class MovableWinRule : WinRuleBase
{
    public override WinRuleResult IsSatisfied(Level level)
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
