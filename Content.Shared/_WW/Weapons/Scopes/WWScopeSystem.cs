using System.Diagnostics.CodeAnalysis;
using Content.Shared._Misfits.Scope;
using Content.Shared._Misfits.Special;
using Content.Shared._Misfits.Weapons.Attachments.Components;
using Content.Shared.Actions;
using Content.Shared.Camera;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Containers;

namespace Content.Shared._WW.Weapons.Scopes;

/// <summary>
/// Scoped guns. Wield the gun and press the scope action: after a do-after (shorter with higher Perception) the
/// view is unlocked and follows the cursor up to <see cref="WWScopeComponent.MaxOffset"/> tiles. The client half is in
/// Content.Client/_WW/Weapons/Scopes; the server widens what it sends to match. Press again, unwield, or take the
/// scope off to lock the view again. Replaces the Misfits fixed-direction zoom scope.
/// </summary>
public sealed partial class WWScopeSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedContentEyeSystem _eye = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedSpecialSystem _special = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    /// <summary>The scope toggle action (the gun provides it; for attached scopes the attachment system forwards it).</summary>
    public const string ScopeAction = "F14ActionToggleScope";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunComponent, ItemWieldedEvent>(OnWielded);
        SubscribeLocalEvent<GunComponent, ItemUnwieldedEvent>(OnUnwielded);
        SubscribeLocalEvent<GunComponent, HeldRelayedEvent<GetEyePvsScaleRelayedEvent>>(OnGetPvsScale);
        SubscribeLocalEvent<WWScopeComponent, GetVerbsEvent<AlternativeVerb>>(OnScopeVerbs);
        SubscribeLocalEvent<FirearmAttachmentHostComponent, GetVerbsEvent<AlternativeVerb>>(OnHostVerbs);
        SubscribeLocalEvent<WWScopeComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<WWScopeComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<WWScopeComponent, ToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<WWScopeComponent, ScopeCycleZoomLevelEvent>(OnCycleAction);
        SubscribeLocalEvent<WWScopeComponent, WWScopeDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<WWScopeComponent, EntGotRemovedFromContainerMessage>(OnRemovedFromGun);
    }

    /// <summary>The scope a gun looks through: its own built-in one, or the one in its optic slot.</summary>
    public bool TryGetScope(EntityUid gun, [NotNullWhen(true)] out Entity<WWScopeComponent>? scope)
    {
        scope = null;
        if (TryComp<WWScopeComponent>(gun, out var own))
        {
            scope = (gun, own);
            return true;
        }

        if (!TryComp<FirearmAttachmentHostComponent>(gun, out var host) || !host.EnableOpticSlot ||
            !_itemSlots.TryGetSlot(gun, host.OpticSlotId, out var slot) || slot.Item is not { } optic ||
            !TryComp<WWScopeComponent>(optic, out var attached))
        {
            return false;
        }

        scope = (optic, attached);
        return true;
    }

    /// <summary>The scope a gun is being aimed through right now: scoped in with the action, and still wielded.</summary>
    public bool TryGetActiveScope(EntityUid gun, [NotNullWhen(true)] out Entity<WWScopeComponent>? scope)
    {
        return TryGetScope(gun, out scope) && scope.Value.Comp.Active &&
               TryComp<WieldableComponent>(gun, out var wield) && wield.Wielded;
    }

    /// <summary>The gun a scope is on: itself if built in, otherwise the gun whose optic slot holds it.</summary>
    public EntityUid? GetGun(EntityUid scope)
    {
        if (HasComp<GunComponent>(scope))
            return scope;
        return _container.TryGetContainingContainer((scope, null, null), out var container) && HasComp<GunComponent>(container.Owner)
            ? container.Owner
            : null;
    }

    /// <summary>Scope-in time: longer for stronger scopes, scaled by Perception (3x at 1, 1x at 5, about instant at 10).</summary>
    public float GetScopeDelay(Entity<WWScopeComponent> scope, EntityUid user)
    {
        var delay = scope.Comp.BaseDelay + scope.Comp.DelayPerMagnification * scope.Comp.Magnification;
        return delay * _special.GetPerceptionScopeDelayMultiplier(user);
    }

    private void OnGetItemActions(Entity<WWScopeComponent> ent, ref GetItemActionsEvent args)
    {
        // Built-in scopes: the gun itself provides the action. Attached scopes get it through the gun's
        // FirearmAttachmentHost (OpticToggleAction), which forwards the event here.
        if (!HasComp<GunComponent>(ent))
            return;

        args.AddAction(ref ent.Comp.ActionEntity, ScopeAction);
        Dirty(ent);
    }

    private void OnToggleAction(Entity<WWScopeComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;
        var user = args.Performer;

        if (ent.Comp.Active)
        {
            SetActive(ent, false, user);
            return;
        }

        if (GetGun(ent) is not { } gun)
            return;

        if (!TryComp<WieldableComponent>(gun, out var wield) || !wield.Wielded)
        {
            _popup.PopupClient(Loc.GetString("ww-scope-popup-wield"), gun, user);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, user, GetScopeDelay(ent, user), new WWScopeDoAfterEvent(), ent, used: gun)
        {
            BreakOnMove = true,
            BreakOnDropItem = true,
            NeedHand = true,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDoAfter(Entity<WWScopeComponent> ent, ref WWScopeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;
        args.Handled = true;

        if (GetGun(ent) is not { } gun || !TryComp<WieldableComponent>(gun, out var wield) || !wield.Wielded)
            return;

        SetActive(ent, true, args.User);
    }

    private void OnCycleAction(Entity<WWScopeComponent> ent, ref ScopeCycleZoomLevelEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;
        Cycle(ent, args.Performer);
    }

    public void SetActive(Entity<WWScopeComponent> scope, bool active, EntityUid? user)
    {
        if (scope.Comp.Active == active)
            return;

        scope.Comp.Active = active;
        Dirty(scope);

        if (user == null)
            return;

        _popup.PopupClient(Loc.GetString(active ? "ww-scope-popup-in" : "ww-scope-popup-out",
            ("mag", FormatMag(scope.Comp.Magnification))), scope, user.Value);
        _eye.UpdatePvsScale(user.Value);
    }

    private void OnWielded(Entity<GunComponent> ent, ref ItemWieldedEvent args)
    {
        if (TryGetScope(ent, out _))
            _eye.UpdatePvsScale(args.User);
    }

    private void OnUnwielded(Entity<GunComponent> ent, ref ItemUnwieldedEvent args)
    {
        if (!TryGetScope(ent, out var scope))
            return;

        // Lowering the gun drops out of the scope.
        if (scope.Value.Comp.Active)
            SetActive(scope.Value, false, null);
        _eye.UpdatePvsScale(args.User);
    }

    private void OnRemovedFromGun(Entity<WWScopeComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (ent.Comp.Active)
            SetActive(ent, false, null);
    }

    private void OnGetPvsScale(Entity<GunComponent> ent, ref HeldRelayedEvent<GetEyePvsScaleRelayedEvent> args)
    {
        if (TryGetActiveScope(ent, out var scope))
            args.Args.Scale += scope.Value.Comp.PvsIncrease;
    }

    private void OnScopeVerbs(Entity<WWScopeComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        AddCycleVerb(ent, args);
    }

    private void OnHostVerbs(Entity<FirearmAttachmentHostComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!HasComp<WWScopeComponent>(ent) && TryGetScope(ent, out var scope))
            AddCycleVerb(scope.Value, args);
    }

    private void AddCycleVerb(Entity<WWScopeComponent> scope, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || scope.Comp.Magnifications.Count < 2)
            return;

        var user = args.User;
        var next = scope.Comp.Magnifications[(scope.Comp.Index + 1) % scope.Comp.Magnifications.Count];
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("ww-scope-verb-adjust", ("mag", FormatMag(next))),
            Priority = -1, // alt-click keeps doing what it did (fire mode, magazine)
            Act = () => Cycle(scope, user),
        });
    }

    public void Cycle(Entity<WWScopeComponent> scope, EntityUid? user)
    {
        if (scope.Comp.Magnifications.Count < 2)
            return;

        scope.Comp.Index = (scope.Comp.Index + 1) % scope.Comp.Magnifications.Count;
        Dirty(scope);

        if (user != null)
        {
            _popup.PopupClient(Loc.GetString("ww-scope-popup-set", ("mag", FormatMag(scope.Comp.Magnification))), scope, user.Value);
            _eye.UpdatePvsScale(user.Value);
        }
    }

    private void OnExamined(Entity<WWScopeComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.Magnifications.Count > 1 ? "ww-scope-examine-variable" : "ww-scope-examine",
            ("mag", FormatMag(ent.Comp.Magnification))));
    }

    public static string FormatMag(float mag) => $"{mag:0.#}x";
}
