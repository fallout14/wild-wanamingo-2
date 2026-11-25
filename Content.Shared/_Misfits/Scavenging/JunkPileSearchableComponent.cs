// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Scavenging/JunkPileSearchableComponent.cs
// Changed for Wild Wanamingo: the loot list lives here (Misfits read a StorageFill that skipped map init, which
// Wizden's StorageFill can't do).

using Content.Shared.Storage;
using Robust.Shared.Serialization;

namespace Content.Shared._Misfits.Scavenging;

/// <summary>
/// Marks a junk pile as searchable for loot, then unavailable until its shared cooldown expires.
/// </summary>
[RegisterComponent]
public sealed partial class JunkPileSearchableComponent : Component
{
    [DataField]
    public float SearchDuration = 3f;

    [DataField]
    public float CooldownSeconds = 3600f;

    /// <summary>
    /// What a search can turn up.
    /// </summary>
    [DataField]
    public List<EntitySpawnEntry> Contents = new();

    public TimeSpan CooldownEnd = TimeSpan.Zero;

    /// <summary>
    /// A Junker Finder already took the extra search of this depleted pile.
    /// </summary>
    public bool FinderSearchUsed;
}

[Serializable, NetSerializable]
public enum JunkPileVisuals : byte
{
    Depleted,
}
