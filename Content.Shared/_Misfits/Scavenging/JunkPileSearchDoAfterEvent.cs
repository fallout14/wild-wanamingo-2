// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Scavenging/JunkPileSearchDoAfterEvent.cs

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Misfits.Scavenging;

/// <summary>
/// Completes a player's search of a junk pile.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class JunkPileSearchDoAfterEvent : SimpleDoAfterEvent
{
    /// <summary>
    /// WW Change - a Junker Finder's extra search of a depleted pile.
    /// </summary>
    [DataField]
    public bool FinderSearch;
}
