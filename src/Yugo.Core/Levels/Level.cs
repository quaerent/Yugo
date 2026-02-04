using Yugo.Core.Entities;
using Yugo.Core.Primitives;
using Yugo.Core.Rules;

namespace Yugo.Core.Levels;

public abstract class Level
{
    private readonly List<Entity> _entities;
    private readonly List<IMergeRule> _mergeRules;
    private readonly List<IWinRule> _winRules;

    protected Level(
        IEnumerable<Entity> entities,
        IEnumerable<IMergeRule> mergeRules,
        IEnumerable<IWinRule> winRules)
    {
        _entities = new List<Entity>(entities);
        _mergeRules = new List<IMergeRule>(mergeRules);
        _winRules = new List<IWinRule>(winRules);
    }

    public IReadOnlyList<Entity> Entities => _entities;
    public IReadOnlyList<IMergeRule> MergeRules => _mergeRules;
    public IReadOnlyList<IWinRule> WinRules => _winRules;

    public abstract void Move(Entity entity, MoveDirection direction);

    public abstract void Tick();

    protected void ApplyMergeRules()
    {
        foreach (var rule in _mergeRules)
        {
            rule.Apply(_entities);
        }
    }

    protected bool IsWin()
    {
        return _winRules.Count == 0 || _winRules.TrueForAll(rule => rule.IsSatisfied(this));
    }
}
