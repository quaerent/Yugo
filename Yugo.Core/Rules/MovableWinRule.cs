using Yugo.Core.Entities;
using Yugo.Core.Game;
using Yugo.Core.Serialization;

namespace Yugo.Core.Rules;

[TypeId("win-movable", "Movable Win")]
public class MovableWinRule : IWinRule
{
    public void Deserialize(Dictionary<string, string> data) { }

    public void Serialize(Dictionary<string, string> data) { }

    public WinRuleResult IsSatisfied(Level level)
    {
        var appearedClusterIds = new HashSet<int>();
        foreach (var entity in level.Entities.OfType<ClusterEntity>())
        {
            if (appearedClusterIds.Contains(entity.ClusterId))
                return WinRuleResult.NotSatisfied;
            appearedClusterIds.Add(entity.ClusterId);
        }
        return WinRuleResult.Satisfied;
    }

    WinRuleResult IWinRule.IsSatisfied(Level level) => IsSatisfied(level);
}
