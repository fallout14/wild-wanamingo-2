namespace Content.Shared._Misfits.Weapons.Attachments.Components;

/// <summary>
/// WW: what mounting a bayonet added to a gun (a melee weapon and right-click melee), so taking the bayonet off
/// removes only that. Server-side bookkeeping.
/// </summary>
[RegisterComponent]
public sealed partial class WWBayonetMeleeComponent : Component
{
    [ViewVariables]
    public bool AddedMelee;

    [ViewVariables]
    public bool AddedAltFire;
}
