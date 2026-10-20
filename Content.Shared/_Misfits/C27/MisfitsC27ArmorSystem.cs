// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/C27/MisfitsC27ArmorSystem.cs
// WW: species come from Wizden's HumanoidProfileComponent; the C-27 species carry the Misfits prefix; ZAX dropped.

using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Misfits.C27;

/// <summary>
/// Only C-27 robots can wear C-27 plating or use C-27 weapons. C-27s seen without an ID show as their chassis name.
/// </summary>
public sealed partial class MisfitsC27ArmorSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly HashSet<ProtoId<SpeciesPrototype>> C27Species =
        ["F14C27", "F14C27NCR", "F14C27BoS"];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MisfitsC27ArmorComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<MisfitsC27WeaponComponent, UseInHandEvent>(OnWeaponUseInHand, before: [typeof(SharedWieldableSystem)]);
        SubscribeLocalEvent<MisfitsC27WeaponComponent, AttemptShootEvent>(OnWeaponAttemptShoot);
        SubscribeLocalEvent<TryGetIdentityShortInfoEvent>(OnTryGetIdentityShortInfo);
    }

    private ProtoId<SpeciesPrototype>? SpeciesOf(EntityUid uid)
    {
        return TryComp<HumanoidProfileComponent>(uid, out var profile) ? profile.Species : (ProtoId<SpeciesPrototype>?) null;
    }

    private void OnTryGetIdentityShortInfo(TryGetIdentityShortInfoEvent args)
    {
        if (args.Handled || SpeciesOf(args.Target) is not { } species || !C27Species.Contains(species))
            return;

        args.Title = Name(args.Target).Trim();
        args.Handled = true;
    }

    private void OnEquipAttempt(Entity<MisfitsC27ArmorComponent> item, ref BeingEquippedAttemptEvent args)
    {
        if (!item.Comp.RequiresC27Species || Allowed(args.EquipTarget, item.Comp.AllowedSpecies))
            return;

        args.Reason = "c27-armor-species-required";
        args.Cancel();
    }

    private void OnWeaponUseInHand(Entity<MisfitsC27WeaponComponent> item, ref UseInHandEvent args)
    {
        // Using it in hand is how it's wielded (WW: Wizden's wield attempt is raised on the user, so this covers it).
        // Let an already-wielded weapon be unwielded.
        if (TryComp<WieldableComponent>(item, out var wieldable) && wieldable.Wielded)
            return;

        if (Allowed(args.User, item.Comp.AllowedSpecies))
            return;

        _popup.PopupClient(Loc.GetString("c27-weapon-species-required"), item, args.User);
        args.Handled = true;
    }

    private void OnWeaponAttemptShoot(Entity<MisfitsC27WeaponComponent> item, ref AttemptShootEvent args)
    {
        if (Allowed(args.User, item.Comp.AllowedSpecies))
            return;

        args.Message = Loc.GetString("c27-weapon-species-required");
        args.Cancelled = true;
    }

    private bool Allowed(EntityUid user, List<ProtoId<SpeciesPrototype>>? only)
    {
        return SpeciesOf(user) is { } species && C27Species.Contains(species) && (only == null || only.Contains(species));
    }
}
