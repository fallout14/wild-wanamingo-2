// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Weapons/Attachments/SharedFirearmAttachmentSystem.cs

using Content.Shared._Misfits.Weapons.Attachments.Components;
using Content.Shared._Misfits.Scope;
using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Melee.Components; // WW Change - bayonet stab
using Content.Shared.Damage; // WW Change
using Content.Shared.Damage.Prototypes; // WW Change
using Content.Shared.Wieldable.Components; // WW Change - bayonet stab
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Content.Shared.Wieldable;
using Content.Shared.Verbs;

namespace Content.Shared._Misfits.Weapons.Attachments;

/// <summary>
/// Applies firearm attachment effects from items installed in a host's attachment slots.
/// Runs in shared code so predicted shots use the same sound, flash, and melee modifiers.
/// </summary>
public sealed partial class SharedFirearmAttachmentSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private ActionContainerSystem _actionContainer = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private Robust.Shared.Network.INetManager _net = default!; // WW Change
    [Dependency] private Robust.Shared.Prototypes.IPrototypeManager _proto = default!; // WW Change

    private readonly HashSet<EntityUid> _pendingRefresh = new();
    private readonly Dictionary<EntityUid, Angle> _baseMeleeAnimationRotations = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FirearmAttachmentHostComponent, ComponentInit>(OnHostInit);
        SubscribeLocalEvent<FirearmAttachmentHostComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<FirearmAttachmentHostComponent, EntRemovedFromContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<FirearmAttachmentHostComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
        SubscribeLocalEvent<FirearmAttachmentHostComponent, GunMuzzleFlashAttemptEvent>(OnMuzzleFlashAttempt);
        SubscribeLocalEvent<FirearmAttachmentHostComponent, GetMeleeDamageEvent>(OnGetMeleeDamage,
            after: [typeof(SharedWieldableSystem)]);
        SubscribeLocalEvent<FirearmAttachmentHostComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<FirearmAttachmentHostComponent, IsGunSuppressedEvent>(OnIsGunSuppressed);
        SubscribeLocalEvent<FirearmAttachmentHostComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<FirearmAttachmentHostComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<FirearmAttachmentHostComponent, ToggleActionEvent>(OnToggleOpticAction);
        SubscribeLocalEvent<FirearmAttachmentHostComponent, ScopeCycleZoomLevelEvent>(OnCycleOpticAction);
        SubscribeLocalEvent<FirearmAttachmentHostComponent, ComponentShutdown>(OnHostShutdown);
        SubscribeLocalEvent<FirearmAttachmentHostComponent, AttemptMeleeEvent>(OnMeleeAttempt); // WW Change - bayonet stab
    }

    private void OnHostInit(Entity<FirearmAttachmentHostComponent> ent, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(ent.Owner, ent.Comp.MuzzleSlotId, ent.Comp.MuzzleSlot);

        if (ent.Comp.EnableOpticSlot)
            _itemSlots.AddItemSlot(ent.Owner, ent.Comp.OpticSlotId, ent.Comp.OpticSlot);

        if (TryComp<MeleeWeaponComponent>(ent, out var melee))
            _baseMeleeAnimationRotations[ent] = melee.WideAnimationRotation;
    }

    // WW Change - bayonet stab, like Wizden's Kardashev-Mosin: right-click (AltFireMelee) with the gun wielded.
    // Our guns have no melee of their own, so mounting a bayonet gives the gun a melee weapon and right-click melee,
    // and taking it off removes what it added. Server only; both components are networked.
    private void AddBayonetMelee(EntityUid gun)
    {
        if (_net.IsClient)
            return;

        var marker = EnsureComp<WWBayonetMeleeComponent>(gun);
        if (!HasComp<MeleeWeaponComponent>(gun))
        {
            var melee = new MeleeWeaponComponent
            {
                Damage = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Blunt"), 5), // replaced by the bayonet's damage
                Angle = Angle.Zero,
                Animation = "WeaponArcThrust",
                WideAnimation = "WeaponArcThrust",
            };
            AddComp(gun, melee);
            marker.AddedMelee = true;
        }

        if (!HasComp<AltFireMeleeComponent>(gun))
        {
            AddComp<AltFireMeleeComponent>(gun);
            marker.AddedAltFire = true;
        }
    }

    private void RemoveBayonetMelee(EntityUid gun)
    {
        if (_net.IsClient || !TryComp<WWBayonetMeleeComponent>(gun, out var marker))
            return;

        if (marker.AddedMelee)
            RemComp<MeleeWeaponComponent>(gun);
        if (marker.AddedAltFire)
            RemComp<AltFireMeleeComponent>(gun);
        RemComp<WWBayonetMeleeComponent>(gun);
    }

    // WW Change - bayonet stab: needs a bayonet, and the gun wielded (like the Mosin's MeleeRequiresWield).
    private void OnMeleeAttempt(Entity<FirearmAttachmentHostComponent> ent, ref AttemptMeleeEvent args)
    {
        if (!TryGetMuzzleAttachment(ent, out var attachment) || !HasComp<BayonetAttachmentComponent>(attachment))
        {
            args.Cancelled = true;
            return;
        }

        if (TryComp<WieldableComponent>(ent, out var wieldable) && !wieldable.Wielded)
        {
            args.Cancelled = true;
            args.Message = Loc.GetString("wieldable-component-requires", ("item", ent.Owner));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var uid in _pendingRefresh)
        {
            if (TryComp<GunComponent>(uid, out var gun))
                _gun.RefreshModifiers((uid, gun));

            RefreshMeleeAnimation(uid);
        }

        _pendingRefresh.Clear();
    }

    private void OnContainerModified(
        Entity<FirearmAttachmentHostComponent> ent,
        ref EntInsertedIntoContainerMessage args)
    {
        RefreshGunIfAttachmentChanged(ent, args.Container.ID);

        if (args.Container.ID == ent.Comp.OpticSlotId)
            RefreshOpticActions(ent);

        if (args.Container.ID == ent.Comp.MuzzleSlotId && HasComp<BayonetAttachmentComponent>(args.Entity)) // WW Change
            AddBayonetMelee(ent);
    }

    private void OnContainerModified(
        Entity<FirearmAttachmentHostComponent> ent,
        ref EntRemovedFromContainerMessage args)
    {
        RefreshGunIfAttachmentChanged(ent, args.Container.ID);

        if (args.Container.ID == ent.Comp.MuzzleSlotId && HasComp<BayonetAttachmentComponent>(args.Entity)) // WW Change
            RemoveBayonetMelee(ent);

        if (args.Container.ID == ent.Comp.OpticSlotId)
            RefreshOpticActions(ent, removing: true);
    }

    private void OnGetItemActions(Entity<FirearmAttachmentHostComponent> ent, ref GetItemActionsEvent args)
    {
        // WW Change - the optic action also drives WW scopes (WWScopeSystem handles the forwarded event)
        if (!TryGetOptic(ent, out var optic) || !IsScope(optic))
            return;

        args.AddAction(ref ent.Comp.OpticToggleActionEntity, ent.Comp.OpticToggleAction);

        if (HasZoomLevels(optic))
            args.AddAction(ref ent.Comp.OpticCycleActionEntity, ent.Comp.OpticCycleAction);

        Dirty(ent);
    }

    private void OnToggleOpticAction(Entity<FirearmAttachmentHostComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled || !TryGetOptic(ent, out var optic))
            return;

        // Forward the rifle-owned proxy action to the actual contained scope.
        args.Handled = true;
        var forwarded = args;
        forwarded.Handled = false;
        RaiseLocalEvent(optic, forwarded);
    }

    private void OnCycleOpticAction(Entity<FirearmAttachmentHostComponent> ent, ref ScopeCycleZoomLevelEvent args)
    {
        if (args.Handled || !TryGetOptic(ent, out var optic))
            return;

        args.Handled = true;
        var forwarded = args;
        forwarded.Handled = false;
        RaiseLocalEvent(optic, forwarded);
    }

    private void RefreshOpticActions(Entity<FirearmAttachmentHostComponent> ent, bool removing = false)
    {
        var user = Transform(ent).ParentUid;
        if (!_hands.IsHolding(user, ent, out _))
            return;

        // The gun is the action provider, so removing or replacing its optic can refresh
        // the granted action without forcing the player to drop and pick the rifle up.
        _actions.RemoveProvidedActions(user, ent);
        if (removing || !TryGetOptic(ent, out var optic) || !IsScope(optic)) // WW Change
            return;

        if (!_actionContainer.EnsureAction(
                ent,
                ref ent.Comp.OpticToggleActionEntity,
                ent.Comp.OpticToggleAction))
        {
            return;
        }

        var actions = new List<EntityUid> { ent.Comp.OpticToggleActionEntity.Value };

        if (HasZoomLevels(optic) && // WW Change
            _actionContainer.EnsureAction(
                ent,
                ref ent.Comp.OpticCycleActionEntity,
                ent.Comp.OpticCycleAction))
        {
            actions.Add(ent.Comp.OpticCycleActionEntity.Value);
        }

        _actions.GrantActions(user, actions, ent.Owner);
        Dirty(ent);
    }

    private void RefreshGunIfAttachmentChanged(Entity<FirearmAttachmentHostComponent> ent, string containerId)
    {
        if (containerId != ent.Comp.MuzzleSlotId && containerId != ent.Comp.OpticSlotId)
            return;

        var visualEvent = new FirearmAttachmentVisualsChangedEvent();
        RaiseLocalEvent(ent, ref visualEvent);

        // Container removal events are raised before the item leaves the slot. Refresh on the
        // next frame so both insertion and removal observe the final container contents.
        _pendingRefresh.Add(ent);
    }

    private void OnGunRefreshModifiers(
        Entity<FirearmAttachmentHostComponent> ent,
        ref GunRefreshModifiersEvent args)
    {
        if (TryGetMuzzleAttachment(ent, out var attachment) &&
            TryComp<SuppressorAttachmentComponent>(attachment, out var suppressor))
        {
            args.SoundGunshot = suppressor.SoundGunshot;
        }
    }

    private void OnMuzzleFlashAttempt(
        Entity<FirearmAttachmentHostComponent> ent,
        ref GunMuzzleFlashAttemptEvent args)
    {
        if (TryGetMuzzleAttachment(ent, out var attachment) &&
            TryComp<SuppressorAttachmentComponent>(attachment, out var suppressor) &&
            suppressor.HideMuzzleFlash)
        {
            args.Cancelled = true;
        }
    }

    private void OnGetMeleeDamage(
        Entity<FirearmAttachmentHostComponent> ent,
        ref GetMeleeDamageEvent args)
    {
        if (TryGetMuzzleAttachment(ent, out var attachment) &&
            TryComp<BayonetAttachmentComponent>(attachment, out var bayonet))
        {
            // A mounted bayonet replaces the rifle-butt bash instead of adding to it.
            // Clear after wield modifiers so no inherited blunt damage remains.
            args.Damage.DamageDict.Clear();
            args.Damage += bayonet.Damage;
        }
    }

    private void OnMeleeHit(
        Entity<FirearmAttachmentHostComponent> ent,
        ref MeleeHitEvent args)
    {
        if (TryGetMuzzleAttachment(ent, out var attachment) &&
            TryComp<BayonetAttachmentComponent>(attachment, out var bayonet))
        {
            args.HitSoundOverride = bayonet.HitSound;
        }
    }

    private void OnIsGunSuppressed(
        Entity<FirearmAttachmentHostComponent> ent,
        ref IsGunSuppressedEvent args)
    {
        if (TryGetMuzzleAttachment(ent, out var attachment) &&
            HasComp<SuppressorAttachmentComponent>(attachment))
        {
            args.Suppressed = true;
        }
    }

    private void OnGetVerbs(
        Entity<FirearmAttachmentHostComponent> ent,
        ref GetVerbsEvent<Verb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        if (_itemSlots.TryGetSlot(ent.Owner, ent.Comp.MuzzleSlotId, out var muzzleSlot) &&
            muzzleSlot.Item is { } muzzleAttachment &&
            _itemSlots.CanEject(ent.Owner, muzzleSlot, user))
        {
            args.Verbs.Add(new Verb
            {
                Text = "Remove muzzle attachment",
                IconEntity = GetNetEntity(muzzleAttachment),
                Act = () => _itemSlots.TryEjectToHands(ent.Owner, muzzleSlot, user, excludeUserAudio: true),
            });
        }

        if (ent.Comp.EnableOpticSlot &&
            _itemSlots.TryGetSlot(ent.Owner, ent.Comp.OpticSlotId, out var opticSlot) &&
            opticSlot.Item is { } opticAttachment &&
            _itemSlots.CanEject(ent.Owner, opticSlot, user))
        {
            args.Verbs.Add(new Verb
            {
                Text = "Remove optic",
                IconEntity = GetNetEntity(opticAttachment),
                Act = () => _itemSlots.TryEjectToHands(ent.Owner, opticSlot, user, excludeUserAudio: true),
            });
        }
    }

    private void OnHostShutdown(
        Entity<FirearmAttachmentHostComponent> ent,
        ref ComponentShutdown args)
    {
        _pendingRefresh.Remove(ent);
        _baseMeleeAnimationRotations.Remove(ent);
        _itemSlots.RemoveItemSlot(ent.Owner, ent.Comp.MuzzleSlot);

        if (ent.Comp.EnableOpticSlot)
            _itemSlots.RemoveItemSlot(ent.Owner, ent.Comp.OpticSlot);
    }

    private void RefreshMeleeAnimation(EntityUid uid)
    {
        if (!TryComp<FirearmAttachmentHostComponent>(uid, out var host) ||
            !TryComp<MeleeWeaponComponent>(uid, out var melee))
        {
            return;
        }

        var rotation = _baseMeleeAnimationRotations.GetValueOrDefault(uid, Angle.Zero);

        if (TryGetMuzzleAttachment((uid, host), out var attachment) &&
            TryComp<BayonetAttachmentComponent>(attachment, out var bayonet))
        {
            rotation = bayonet.AnimationRotation;
        }

        if (melee.WideAnimationRotation.Equals(rotation))
            return;

        melee.WideAnimationRotation = rotation;
        Dirty(uid, melee);
    }

    private bool TryGetMuzzleAttachment(
        Entity<FirearmAttachmentHostComponent> ent,
        out EntityUid attachment)
    {
        attachment = default;

        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.MuzzleSlotId, out var slot) ||
            slot.Item is not { } item)
        {
            return false;
        }

        attachment = item;
        return true;
    }

    // WW Change - Misfits scopes and WW scopes both get the optic actions
    private bool IsScope(EntityUid optic) =>
        HasComp<ScopeComponent>(optic) || HasComp<Content.Shared._WW.Weapons.Scopes.WWScopeComponent>(optic);

    private bool HasZoomLevels(EntityUid optic) =>
        TryComp<ScopeComponent>(optic, out var scope) && scope.ZoomLevels.Count > 1 ||
        TryComp<Content.Shared._WW.Weapons.Scopes.WWScopeComponent>(optic, out var ww) && ww.Magnifications.Count > 1;

    private bool TryGetOptic(Entity<FirearmAttachmentHostComponent> ent, out EntityUid optic)
    {
        optic = default;

        if (!ent.Comp.EnableOpticSlot ||
            !_itemSlots.TryGetSlot(ent.Owner, ent.Comp.OpticSlotId, out var slot) ||
            slot.Item is not { } item)
        {
            return false;
        }

        optic = item;
        return true;
    }
}

/// <summary>
/// Raised on a gun to determine whether systems such as distant gunshot audio should treat it as suppressed.
/// </summary>
[ByRefEvent]
public record struct IsGunSuppressedEvent(bool Suppressed = false);

/// <summary>
/// Raised when a firearm's installed attachment visuals need to be refreshed.
/// </summary>
[ByRefEvent]
public record struct FirearmAttachmentVisualsChangedEvent;
