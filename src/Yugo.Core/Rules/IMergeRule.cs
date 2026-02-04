using Yugo.Core.Entities;

namespace Yugo.Core.Rules;

public interface IMergeRule
{
    void Apply(IList<Entity> entities);
}
