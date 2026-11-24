#nullable disable
using System.Linq;
using Content.Server.Destructible.Thresholds;
using Content.Shared.Destructible.Thresholds.Triggers;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Server.Destructible;

public sealed class DamageThresholdsSerializer : ITypeSerializer<List<DamageThreshold>, SequenceDataNode>
{
    // WW Change start: opt out of inherited thresholds.
    /// <summary>
    /// A bare <c>- replaceInherited</c> entry in a prototype's thresholds list. [AlwaysPushInheritance] appends the
    /// parents' thresholds after the child's own, so everything after this marker came from parents and is dropped:
    /// the child's list replaces its parents' the way it did before Wizden #44644. Used by prototypes ported from
    /// N14/Misfits, which were written for that.
    /// </summary>
    public const string ReplaceInherited = "replaceInherited";

    private static bool IsReplaceMarker(DataNode node) => node is ValueDataNode { Value: ReplaceInherited };

    /// <summary>The entries that count: everything before the first <see cref="ReplaceInherited"/> marker.</summary>
    private static List<DataNode> Effective(SequenceDataNode node)
    {
        var list = new List<DataNode>(node.Sequence.Count);
        foreach (var elem in node.Sequence)
        {
            if (IsReplaceMarker(elem))
                break;
            list.Add(elem);
        }
        return list;
    }
    // WW Change end
    public ValidationNode Validate(ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext context = null)
    {
        var list = new List<ValidationNode>();
        foreach (var elem in node.Sequence)
        {
            if (IsReplaceMarker(elem)) // WW Change
                continue;
            list.Add(serializationManager.ValidateNode<DamageThreshold>(elem, context));
        }

        return new ValidatedSequenceNode(list);
    }

    public List<DamageThreshold> Read(ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext context = null,
        ISerializationManager.InstantiationDelegate<List<DamageThreshold>> instanceProvider = null)
    {
        if (instanceProvider != null)
        {
            var sawmill = dependencies.Resolve<ILogManager>().GetSawmill("szr");
            sawmill.Warning($"Provided value to a Read-call for a {nameof(List<DamageThreshold>)}. Ignoring...");
        }

        var list = new List<DamageThreshold>();
        var sequence = Effective(node); // WW Change

        var ignore = new HashSet<int>(sequence.Count);
        for (var i = 0; i < sequence.Count; i++)
        {
            if (ignore.Contains(i))
                continue;

            var item = serializationManager.Read<DamageThreshold>(sequence[i], hookCtx, context);
            for (var j = i + 1; j < sequence.Count; j++)
            {
                var item2 = serializationManager.Read<DamageThreshold>(sequence[j], hookCtx, context);
                if (IsEqual(item, item2))
                {
                    item.Behaviors = item.Behaviors.Union(item2.Behaviors).ToList();
                    ignore.Add(j);
                }
            }

            list.Add(item);
        }

        list.Sort();
        return list;
    }

    public DataNode Write(ISerializationManager serializationManager,
        List<DamageThreshold> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext context = null)
    {
        var sequence = new SequenceDataNode();

        foreach (var elem in value)
        {
            sequence.Add(serializationManager.WriteValue(elem, alwaysWrite, context));
        }

        return sequence;
    }

    private bool IsEqual(DamageThreshold a, DamageThreshold b)
    {
        return IsEqual(a.Trigger, b.Trigger);
    }

    private bool IsEqual<T1, T2>(T1 a, T2 b) where T1 : IThresholdTrigger where T2 : IThresholdTrigger
    {
        // Ensure same type and same thresholds!
        return a is T2 trigger && trigger.Equals(b);
    }
}
