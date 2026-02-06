using Yugo.Core.Game;
using Yugo.Core.Serialization;

namespace Yugo.Core.Rules;

public abstract class MergeRuleBase : IMergeRule, ISnapshotSerializable
{
    public Snapshot Snapshot()
    {
        var data = new Dictionary<string, string>();
        Serialize(data);
        return new Snapshot(TypeIdAttribute.GetId(GetType()), data);
    }

    public void ApplySnapshot(Snapshot snapshot)
    {
        Deserialize(snapshot.Data);
    }

    public virtual void Serialize(Dictionary<string, string> data) { }

    public virtual void Deserialize(Dictionary<string, string> data) { }

    public abstract void Apply(Level level);
}

public abstract class WinRuleBase : IWinRule, ISnapshotSerializable
{
    public Snapshot Snapshot()
    {
        var data = new Dictionary<string, string>();
        Serialize(data);
        return new Snapshot(TypeIdAttribute.GetId(GetType()), data);
    }

    public void ApplySnapshot(Snapshot snapshot)
    {
        Deserialize(snapshot.Data);
    }

    public virtual void Serialize(Dictionary<string, string> data) { }

    public virtual void Deserialize(Dictionary<string, string> data) { }

    public abstract WinRuleResult IsSatisfied(Level level);
}
