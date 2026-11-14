// WW Change - armour penetration, re-ported from Misfits (Content.Shared/Armor/SharedArmorSystem.cs).
// The rebase porter dropped Projectile.armorPenetration because it was a core edit; this restores it.

using Content.Shared.Damage;

namespace Content.Shared._WW.Armor;

public static class ArmorPenetrationHelper
{
    /// <summary>
    /// Scales an armour modifier set by a projectile's armour penetration.
    /// </summary>
    /// <remarks>
    /// Positive values (AP rounds) push each coefficient toward 1 and shrink flat reductions:
    /// 0.35 on a 0.6 coefficient gives 0.74, and 1 ignores the armour completely.
    /// Negative values (hollow points) push coefficients toward 0 and grow flat reductions:
    /// -0.3 on a 0.6 coefficient gives 0.48, and flat reductions are multiplied by 1.3.
    /// The value is clamped to [-1, 1]. Coefficients above 1 (weaknesses) are left alone,
    /// so penetration never makes a weakness worse or cancels it.
    /// </remarks>
    public static DamageModifierSet Apply(DamageModifierSet modifiers, float penetration)
    {
        penetration = Math.Clamp(penetration, -1f, 1f);

        if (penetration == 0f)
            return modifiers;

        var effective = new DamageModifierSet();

        foreach (var (type, coefficient) in modifiers.Coefficients)
        {
            effective.Coefficients[type] = coefficient >= 1f
                ? coefficient
                : MathF.Max(0f, coefficient + (1f - coefficient) * penetration);
        }

        foreach (var (type, flat) in modifiers.FlatReductions)
        {
            effective.FlatReductions[type] = flat * (1f - penetration);
        }

        return effective;
    }
}
