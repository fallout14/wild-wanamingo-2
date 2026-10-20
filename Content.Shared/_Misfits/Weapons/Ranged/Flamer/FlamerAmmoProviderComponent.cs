// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Weapons/Ranged/Flamer/FlamerAmmoProviderComponent.cs
// Changed for Wild Wanamingo: shots are hitscan entities (ShotPrototype) so Wizden's gun code needs no flamer case.

using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Misfits.Weapons.Ranged.Flamer;

/// <summary>
/// Fires a line of tile fires, paid for out of a fuel solution.
/// </summary>
[RegisterComponent]
public sealed partial class FlamerAmmoProviderComponent : AmmoProviderComponent
{
    [DataField]
    public string SolutionId = "chamber";

    [DataField]
    public float FireCost = 5f;

    [DataField]
    public EntProtoId FireTilePrototype = "F14TileFire";

    /// <summary>
    /// Optional cosmetic projectile fired along with the fire line.
    /// </summary>
    [DataField]
    public EntProtoId? ProjectileProto;

    [DataField]
    public int Range = 6;

    /// <summary>
    /// Hitscan entity spawned per shot; it carries the fire settings to the server.
    /// </summary>
    [DataField]
    public EntProtoId ShotPrototype = "F14FlamerShot";
}
