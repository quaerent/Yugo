namespace Yugo.Core.Rules;

public interface IWinRule
{
    bool IsSatisfied(Levels.Level level);
}
