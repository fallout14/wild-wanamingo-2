// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Weapons/Ranged/Flamer/FlamerShot.cs
// Changed for Wild Wanamingo: a component on a hitscan entity instead of a custom IShootable.

using Robust.Shared.Prototypes;

namespace Content.Shared._Misfits.Weapons.Ranged.Flamer;

/// <summary>
/// One flamer shot. Spawned by <see cref="FlamerAmmoProviderComponent"/> with the gun's fire settings,
/// then turned into a line of tile fires when the hitscan is traced.
/// </summary>
[RegisterComponent]
public sealed partial class FlamerShotComponent : Component
{
    [DataField]
    public EntProtoId FireTilePrototype = "F14TileFire";

    [DataField]
    public EntProtoId? ProjectileProto;

    [DataField]
    public int Range = 6;
}
