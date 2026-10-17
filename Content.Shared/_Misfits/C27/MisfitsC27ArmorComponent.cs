// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/C27/MisfitsC27ArmorComponent.cs,
// MisfitsC27WeaponComponent.cs

using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Misfits.C27;

/// <summary>
/// Clothing only C-27 robots may wear. (C-27s wearing other outerwear is blocked by their inventory template's
/// MisfitsC27Wearable whitelist.)
/// </summary>
[RegisterComponent]
public sealed partial class MisfitsC27ArmorComponent : Component
{
    /// <summary>
    /// False for trophy/admin variants anyone may wear.
    /// </summary>
    [DataField]
    public bool RequiresC27Species = true;

    /// <summary>
    /// Narrows it to some C-27 models (e.g. only the NCR one). Null: any C-27.
    /// </summary>
    [DataField]
    public List<ProtoId<SpeciesPrototype>>? AllowedSpecies;
}

/// <summary>
/// A weapon only C-27 robots can wield or fire.
/// </summary>
[RegisterComponent]
public sealed partial class MisfitsC27WeaponComponent : Component
{
    [DataField]
    public List<ProtoId<SpeciesPrototype>>? AllowedSpecies;
}
