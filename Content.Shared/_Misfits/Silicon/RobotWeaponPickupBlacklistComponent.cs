// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Silicon/RobotWeaponPickupBlacklistComponent.cs

using Content.Shared.Whitelist;

namespace Content.Shared._Misfits.Silicon;

/// <summary>
/// Player robots keep their hands for utility items but can't pick up anything matching this blacklist (guns).
/// </summary>
[RegisterComponent]
public sealed partial class RobotWeaponPickupBlacklistComponent : Component
{
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// WW Change - off for chassis that are allowed weapons (Robobrains).
    /// </summary>
    [DataField]
    public bool Enabled = true;
}
