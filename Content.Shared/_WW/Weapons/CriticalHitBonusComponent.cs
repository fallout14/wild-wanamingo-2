using Robust.Shared.GameStates;

namespace Content.Shared._WW.Weapons;

/// <summary>
/// Gives a gun or melee weapon its own critical hit bonus on top of the wielder's Luck (Misfits SPECIAL crits,
/// see SpecialCombatSystem). For a gun it applies to every bullet, pellet or beam it fires.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CriticalHitBonusComponent : Component
{
    /// <summary>
    /// Added to the per-hit crit chance after Luck, e.g. 0.05 = +5%. Applies even at low Luck.
    /// </summary>
    [DataField]
    public float Chance;

    /// <summary>
    /// Replaces the crit damage multiplier (SPECIAL tuning default x1.5) for this weapon's crits. Null keeps the default.
    /// </summary>
    [DataField]
    public float? DamageMultiplier;
}
