using System.Linq;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;

namespace Content.Shared._WW.Traits;

/// <summary>
/// WW: the Misfits talent tree rules shared by the character editor and profile validation. All traits draw on one
/// point pool (a negative cost gives points back), and a trait with a prerequisite needs that trait taken too.
/// Per-category caps (Wizden's speech traits) still apply on top.
/// </summary>
public static class TraitTree
{
    /// <summary>
    /// Points every character has to spend (Misfits' game.traits_default_points).
    /// </summary>
    public const int PointBudget = 10;

    public static int SpentPoints(IEnumerable<ProtoId<TraitPrototype>> traits, IPrototypeManager protoManager)
    {
        var spent = 0;
        foreach (var trait in traits)
        {
            if (protoManager.TryIndex(trait, out var proto))
                spent += proto.Cost;
        }

        return spent;
    }

    /// <summary>
    /// Drops traits whose prerequisite isn't taken, then trims the most expensive picks until the pool isn't
    /// overspent (and drops whatever depended on them).
    /// </summary>
    public static List<ProtoId<TraitPrototype>> Prune(IEnumerable<ProtoId<TraitPrototype>> traits,
        IPrototypeManager protoManager)
    {
        var kept = traits.Where(t => protoManager.HasIndex(t)).Distinct().ToList();

        while (true)
        {
            var set = kept.ToHashSet();
            var orphaned = kept.FindIndex(t =>
                protoManager.Index(t).Prerequisite is { } prereq && !set.Contains(prereq));

            if (orphaned >= 0)
            {
                kept.RemoveAt(orphaned);
                continue;
            }

            if (SpentPoints(kept, protoManager) <= PointBudget || kept.Count == 0)
                return kept;

            var priciest = kept.MaxBy(t => protoManager.Index(t).Cost);
            if (protoManager.Index(priciest).Cost <= 0)
                return kept;

            kept.Remove(priciest);
        }
    }

    /// <summary>
    /// Whether <paramref name="trait"/> can be added to <paramref name="owned"/>.
    /// </summary>
    public static bool CanTake(IReadOnlySet<ProtoId<TraitPrototype>> owned, TraitPrototype trait,
        IPrototypeManager protoManager)
    {
        if (trait.Prerequisite is { } prereq && !owned.Contains(prereq))
            return false;

        return SpentPoints(owned, protoManager) + trait.Cost <= PointBudget;
    }

    /// <summary>
    /// <paramref name="trait"/> and everything that depends on it, directly or not.
    /// </summary>
    public static HashSet<ProtoId<TraitPrototype>> WithDependents(IEnumerable<ProtoId<TraitPrototype>> owned,
        ProtoId<TraitPrototype> trait, IPrototypeManager protoManager)
    {
        var result = new HashSet<ProtoId<TraitPrototype>> { trait };
        var ownedList = owned.ToList();
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var other in ownedList)
            {
                if (result.Contains(other) || !protoManager.TryIndex(other, out var proto))
                    continue;

                if (proto.Prerequisite is { } prereq && result.Contains(prereq))
                    changed |= result.Add(other);
            }
        }

        return result;
    }
}
