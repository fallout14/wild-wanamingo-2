// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Server/_Misfits/PowerArmor/PowerArmorEmpSystem.cs
// Changed for Wild Wanamingo: a flat shock (Misfits scaled it by EMP strength with its own EMP helper).

using Content.Shared.Emp;
using Content.Shared._Misfits.PowerArmor;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server._Misfits.PowerArmor;

/// <summary>
/// Applies additional, resistance-bypassing Shock damage when an EMP pulse reaches
/// a character currently wearing power armor.
/// </summary>
public sealed partial class PowerArmorEmpSystem : EntitySystem
{
    // Power armor still conducts a significant, resistance-bypassing EMP shock into its wearer.
    private const float EmpShockDamage = 50f;

    private static readonly ProtoId<DamageTypePrototype> ShockDamage = "Shock";

    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PowerArmorWornComponent, EmpPulseEvent>(OnEmpPulse);
    }

    private void OnEmpPulse(Entity<PowerArmorWornComponent> ent, ref EmpPulseEvent args)
    {
        if (!_prototypes.TryIndex(ShockDamage, out var shockPrototype))
            return;

        // The armor is the immediate source because it conducts the EMP into its wearer.
        var damage = new DamageSpecifier(
            shockPrototype,
            EmpShockDamage);
        _damageable.TryChangeDamage(
            ent.Owner,
            damage,
            ignoreResistances: true,
            origin: ent.Comp.Armor);

        args.Affected = true;
        _popup.PopupEntity(
            Loc.GetString("power-armor-emp-conduction"),
            ent.Owner,
            ent.Owner,
            PopupType.LargeCaution);
    }
}
