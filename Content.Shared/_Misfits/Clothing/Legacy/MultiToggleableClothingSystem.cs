// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/Clothing/EntitySystems/ToggleableClothingSystem.cs
// (Misfits' copy of a Goobstation/Space Station 14 file.) Renamed Multi* so it lives next to Wizden's
// single-piece ToggleableClothing: power armor deploys several pieces from one suit.

using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Strip;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared._Misfits.Clothing.Legacy;

// GOOBSTATION - MODSUITS - THIS SYSTEM FULLY CHANGED
public sealed partial class MultiToggleableClothingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _netMan = default!;
    [Dependency] private SharedContainerSystem _containerSystem = default!;
    [Dependency] private SharedActionsSystem _actionsSystem = default!;
    [Dependency] private ActionContainerSystem _actionContainer = default!;
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedStrippableSystem _strippable = default!;
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultiToggleableClothingComponent, ComponentInit>(OnToggleableInit);
        SubscribeLocalEvent<MultiToggleableClothingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MultiToggleableClothingComponent, MultiToggleClothingEvent>(OnToggleClothingAction);
        SubscribeLocalEvent<MultiToggleableClothingComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<MultiToggleableClothingComponent, ComponentRemove>(OnRemoveToggleable);
        SubscribeLocalEvent<MultiToggleableClothingComponent, GotUnequippedEvent>(OnToggleableUnequip);
        SubscribeLocalEvent<MultiToggleableClothingComponent, MultiToggleableClothingUiMessage>(OnToggleClothingMessage);
        SubscribeLocalEvent<MultiToggleableClothingComponent, BeingUnequippedAttemptEvent>(OnToggleableUnequipAttempt);

        SubscribeLocalEvent<MultiAttachedClothingComponent, ComponentInit>(OnAttachedInit);
        SubscribeLocalEvent<MultiAttachedClothingComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<MultiAttachedClothingComponent, GotUnequippedEvent>(OnAttachedUnequip);
        SubscribeLocalEvent<MultiAttachedClothingComponent, ComponentRemove>(OnRemoveAttached);
        SubscribeLocalEvent<MultiAttachedClothingComponent, BeingUnequippedAttemptEvent>(OnAttachedUnequipAttempt);

        SubscribeLocalEvent<MultiToggleableClothingComponent, InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>>>(GetRelayedVerbs);
        SubscribeLocalEvent<MultiToggleableClothingComponent, GetVerbsEvent<EquipmentVerb>>(OnGetVerbs);
        SubscribeLocalEvent<MultiAttachedClothingComponent, GetVerbsEvent<EquipmentVerb>>(OnGetAttachedStripVerbsEvent);
        SubscribeLocalEvent<MultiToggleableClothingComponent, MultiToggleClothingDoAfterEvent>(OnDoAfterComplete);
    }

    private void GetRelayedVerbs(Entity<MultiToggleableClothingComponent> toggleable, ref InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>> args)
    {
        OnGetVerbs(toggleable, ref args.Args);
    }

    private void OnGetVerbs(Entity<MultiToggleableClothingComponent> toggleable, ref GetVerbsEvent<EquipmentVerb> args)
    {
        var comp = toggleable.Comp;

        if (!args.CanAccess || !args.CanInteract || args.Hands == null || comp.ClothingUids.Count == 0 || comp.Container == null)
            return;

        var text = comp.VerbText ?? (comp.ActionEntity == null ? null : Name(comp.ActionEntity.Value));
        if (text == null)
            return;

        if (!_inventorySystem.InSlotWithFlags(toggleable.Owner, comp.RequiredFlags))
            return;

        var wearer = Transform(toggleable).ParentUid;
        if (args.User != wearer && comp.StripDelay == null)
            return;

        var user = args.User;

        var verb = new EquipmentVerb()
        {
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/outfit.svg.192dpi.png")),
            Text = Loc.GetString(text),
        };

        if (user == wearer)
        {
            verb.Act = () => ToggleClothing(user, toggleable);
        }
        else
        {
            verb.Act = () => StartDoAfter(user, toggleable, wearer);
        }

        args.Verbs.Add(verb);
    }

    private void StartDoAfter(EntityUid user, Entity<MultiToggleableClothingComponent> toggleable, EntityUid wearer)
    {
        var comp = toggleable.Comp;

        if (comp.StripDelay == null)
            return;

        var (time, stealth) = _strippable.GetStripTimeModifiers(user, wearer, toggleable, comp.StripDelay.Value);

        bool hidden = stealth;

        var args = new DoAfterArgs(EntityManager, user, time, new MultiToggleClothingDoAfterEvent(), toggleable, wearer, toggleable)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            DistanceThreshold = 2,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        if (!hidden)
        {
            var popup = Loc.GetString("strippable-component-alert-owner-interact", ("user", Identity.Entity(user, EntityManager)), ("item", toggleable));
            _popupSystem.PopupEntity(popup, wearer, wearer, PopupType.Large);
        }
    }

    private void OnGetAttachedStripVerbsEvent(Entity<MultiAttachedClothingComponent> attached, ref GetVerbsEvent<EquipmentVerb> args)
    {
        var comp = attached.Comp;

        if (!TryComp<MultiToggleableClothingComponent>(comp.AttachedUid, out var toggleableComp))
            return;

        // redirect to the attached entity.
        OnGetVerbs((comp.AttachedUid, toggleableComp), ref args);
    }

    private void OnDoAfterComplete(Entity<MultiToggleableClothingComponent> toggleable, ref MultiToggleClothingDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        ToggleClothing(args.User, toggleable);
    }

    private void OnInteractHand(Entity<MultiAttachedClothingComponent> attached, ref InteractHandEvent args)
    {
        var comp = attached.Comp;

        if (args.Handled)
            return;

        if (!TryComp(comp.AttachedUid, out MultiToggleableClothingComponent? toggleableComp)
            || toggleableComp.Container == null)
            return;

        // Get slot from dictionary of uid-slot
        if (!toggleableComp.ClothingUids.TryGetValue(attached.Owner, out var attachedSlot))
            return;

        if (!_inventorySystem.TryUnequip(Transform(attached.Owner).ParentUid, attachedSlot, force: true))
            return;

        _containerSystem.Insert(attached.Owner, toggleableComp.Container);
        args.Handled = true;
    }

    /// <summary>
    /// Prevents from unequipping entity if all attached not unequipped
    /// </summary>
    private void OnToggleableUnequipAttempt(Entity<MultiToggleableClothingComponent> toggleable, ref BeingUnequippedAttemptEvent args)
    {
        var comp = toggleable.Comp;

        if (!comp.BlockUnequipWhenAttached)
            return;

        if (GetAttachedToggleStatus(toggleable) == MultiToggleableClothingAttachedStatus.NoneToggled)
            return;

        _popupSystem.PopupClient(Loc.GetString("toggleable-clothing-remove-all-attached-first"), args.UnEquipTarget, args.UnEquipTarget);

        args.Cancel();
    }

    /// <summary>
    ///     Called when the suit is unequipped, to ensure that the helmet also gets unequipped.
    /// </summary>
    private void OnToggleableUnequip(Entity<MultiToggleableClothingComponent> toggleable, ref GotUnequippedEvent args)
    {
        var comp = toggleable.Comp;

        // If it's a part of PVS departure then don't handle it.
        if (_timing.ApplyingState)
            return;

        // Check if container exists and we have linked clothings
        if (comp.Container == null || comp.ClothingUids.Count == 0)
            return;

        var parts = comp.ClothingUids;

        foreach (var part in parts)
        {
            // Check if entity in container what means it already unequipped
            if (comp.Container.Contains(part.Key) || part.Value == null)
                continue;

            _inventorySystem.TryUnequip(args.EquipTarget, part.Value, force: true);
        }
    }

    private void OnRemoveToggleable(Entity<MultiToggleableClothingComponent> toggleable, ref ComponentRemove args)
    {
        // If the parent/owner component of the attached clothing is being removed (entity getting deleted?) we will
        // delete the attached entity. We do this regardless of whether or not the attached entity is currently
        // "outside" of the container or not. This means that if a hardsuit takes too much damage, the helmet will also
        // automatically be deleted.

        var comp = toggleable.Comp;

        _actionsSystem.RemoveAction(comp.ActionEntity);

        if (comp.ClothingUids == null || _netMan.IsClient)
            return;

        foreach (var clothing in comp.ClothingUids.Keys)
            QueueDel(clothing);
    }

    private void OnAttachedUnequipAttempt(Entity<MultiAttachedClothingComponent> attached, ref BeingUnequippedAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnRemoveAttached(Entity<MultiAttachedClothingComponent> attached, ref ComponentRemove args)
    {
        // if the attached component is being removed (maybe entity is being deleted?) we will just remove the
        // toggleable clothing component. This means if you had a hard-suit helmet that took too much damage, you would
        // still be left with a suit that was simply missing a helmet. There is currently no way to fix a partially
        // broken suit like this.

        var comp = attached.Comp;

        if (!TryComp(comp.AttachedUid, out MultiToggleableClothingComponent? toggleableComp)
            || toggleableComp.LifeStage > ComponentLifeStage.Running)
            return;

        var clothingUids = toggleableComp.ClothingUids;

        if (!clothingUids.Remove(attached.Owner) || clothingUids.Count > 0)
            return;

        // If no attached clothing left - remove component and action
        if (clothingUids.Count > 0)
            return;

        _actionsSystem.RemoveAction(toggleableComp.ActionEntity);
        RemComp(comp.AttachedUid, toggleableComp);
    }

    /// <summary>
    ///     Called if the clothing was unequipped, to ensure that it gets moved into the suit's container.
    /// </summary>
    private void OnAttachedUnequip(Entity<MultiAttachedClothingComponent> attached, ref GotUnequippedEvent args)
    {
        var comp = attached.Comp;

        // Death told me to do this- if you need to figure out why each of these are here, idk, figure it out.
        if (_timing.ApplyingState
            || comp.LifeStage > ComponentLifeStage.Running
            || !TryComp(comp.AttachedUid, out MultiToggleableClothingComponent? toggleableComp)
            || toggleableComp.LifeStage > ComponentLifeStage.Running
            || !toggleableComp.ClothingUids.ContainsKey(attached.Owner))
            return;

        if (toggleableComp.Container != null)
            _containerSystem.Insert(attached.Owner, toggleableComp.Container);
    }

    /// <summary>
    ///     Equip or unequip toggle clothing with ui message
    /// </summary>
    private void OnToggleClothingMessage(Entity<MultiToggleableClothingComponent> toggleable, ref MultiToggleableClothingUiMessage args)
    {
        var attachedUid = GetEntity(args.AttachedClothingUid);

        ToggleClothing(args.Actor, toggleable, attachedUid);
    }

    /// <summary>
    ///     Equip or unequip the toggleable clothing.
    /// </summary>
    private void OnToggleClothingAction(Entity<MultiToggleableClothingComponent> toggleable, ref MultiToggleClothingEvent args)
    {
        var comp = toggleable.Comp;

        if (args.Handled)
            return;

        if (comp.Container == null || comp.ClothingUids.Count == 0)
            return;

        args.Handled = true;

        // If clothing have only one attached clothing (like helmets) action will just toggle it
        // If it have more attached clothings, it'll open radial menu
        if (comp.ClothingUids.Count == 1)
            ToggleClothing(args.Performer, toggleable, comp.ClothingUids.First().Key);
        else
            _uiSystem.OpenUi(toggleable.Owner, MultiToggleClothingUiKey.Key, args.Performer);
    }

    /// <summary>
    ///     Toggle function for single clothing
    /// </summary>
    public void ToggleClothing(EntityUid user, Entity<MultiToggleableClothingComponent> toggleable, EntityUid attachedUid) // WW Change - public for tests
    {
        var comp = toggleable.Comp;
        var attachedClothings = comp.ClothingUids;
        var container = comp.Container;
        var previousStatus = GetAttachedToggleStatus(toggleable, comp);

        if (!CanToggleClothing(user, toggleable))
            return;

        if (!attachedClothings.TryGetValue(attachedUid, out var slot) || string.IsNullOrEmpty(slot))
            return;

        if (!container!.Contains(attachedUid))
            UnequipClothing(user, toggleable, attachedUid, slot!);
        else
            EquipClothing(user, toggleable, attachedUid, slot!);

        RaiseToggleableClothingToggledEvent(user, toggleable, previousStatus);
    }

    /// <summary>
    ///     Toggle function for toggling multiple clothings at once
    /// </summary>
    private void ToggleClothing(EntityUid user, Entity<MultiToggleableClothingComponent> toggleable)
    {
        var comp = toggleable.Comp;
        var attachedClothings = comp.ClothingUids;
        var container = comp.Container;
        var previousStatus = GetAttachedToggleStatus(toggleable, comp);

        if (!CanToggleClothing(user, toggleable))
            return;

        if (GetAttachedToggleStatus(toggleable, comp) == MultiToggleableClothingAttachedStatus.NoneToggled)
            foreach (var clothing in attachedClothings)
                EquipClothing(user, toggleable, clothing.Key, clothing.Value);
        else
            foreach (var clothing in attachedClothings)
                if (!container!.Contains(clothing.Key))
                    UnequipClothing(user, toggleable, clothing.Key, clothing.Value);

        RaiseToggleableClothingToggledEvent(user, toggleable, previousStatus);
    }

    // #Misfits Change /Add/: Raise a single event only when a toggleable clothing item fully deploys or fully retracts.
    private void RaiseToggleableClothingToggledEvent(
        EntityUid user,
        Entity<MultiToggleableClothingComponent> toggleable,
        MultiToggleableClothingAttachedStatus previousStatus)
    {
        var currentStatus = GetAttachedToggleStatus(toggleable, toggleable.Comp);

        if (previousStatus == currentStatus)
            return;

        if (previousStatus == MultiToggleableClothingAttachedStatus.NoneToggled
            && currentStatus == MultiToggleableClothingAttachedStatus.AllToggled)
        {
            RaiseLocalEvent(toggleable.Owner, new MultiToggleableClothingToggledEvent(user, true));
            return;
        }

        if (previousStatus == MultiToggleableClothingAttachedStatus.AllToggled
            && currentStatus == MultiToggleableClothingAttachedStatus.NoneToggled)
            RaiseLocalEvent(toggleable.Owner, new MultiToggleableClothingToggledEvent(user, false));
    }

    private bool CanToggleClothing(EntityUid user, Entity<MultiToggleableClothingComponent> toggleable)
    {
        var comp = toggleable.Comp;
        var attachedClothings = comp.ClothingUids;
        var container = comp.Container;

        if (container == null || attachedClothings.Count == 0)
            return false;

        var ev = new MultiToggleClothingAttemptEvent(user, toggleable);
        RaiseLocalEvent(toggleable, ev);

        return !ev.Cancelled;
    }

    private void UnequipClothing(EntityUid user, Entity<MultiToggleableClothingComponent> toggleable, EntityUid clothing, string slot)
    {
        var parent = Transform(toggleable.Owner).ParentUid;

        _inventorySystem.TryUnequip(user, parent, slot, force: true);

        // If attached have clothing in container - equip it
        if (!TryComp<MultiAttachedClothingComponent>(clothing, out var attachedComp) || attachedComp.ClothingContainer == null)
            return;

        var storedClothing = attachedComp.ClothingContainer.ContainedEntity;

        if (storedClothing != null)
            _inventorySystem.TryEquip(parent, storedClothing.Value, slot, force: true);
    }
    private void EquipClothing(EntityUid user, Entity<MultiToggleableClothingComponent> toggleable, EntityUid clothing, string slot)
    {
        var parent = Transform(toggleable.Owner).ParentUid;
        var comp = toggleable.Comp;

        if (_inventorySystem.TryGetSlotEntity(parent, slot, out var currentClothing))
        {
            // Check if we need to replace current clothing
            if (!TryComp<MultiAttachedClothingComponent>(clothing, out var attachedComp) || !comp.ReplaceCurrentClothing)
            {
                _popupSystem.PopupClient(Loc.GetString("toggleable-clothing-remove-first", ("entity", currentClothing)), user, user);
                return;
            }

            // Check if attached clothing have container or this container not empty
            if (attachedComp.ClothingContainer == null || attachedComp.ClothingContainer.ContainedEntity != null)
                return;

            if (_inventorySystem.TryUnequip(user, parent, slot))
                _containerSystem.Insert(currentClothing.Value, attachedComp.ClothingContainer);
        }

        _inventorySystem.TryEquip(user, parent, clothing, slot);
    }

    private void OnGetActions(Entity<MultiToggleableClothingComponent> toggleable, ref GetItemActionsEvent args)
    {
        var comp = toggleable.Comp;

        if (comp.ClothingUids.Count == 0 || comp.ActionEntity == null || args.SlotFlags != comp.RequiredFlags)
            return;

        args.AddAction(comp.ActionEntity.Value);
    }

    private void OnToggleableInit(Entity<MultiToggleableClothingComponent> toggleable, ref ComponentInit args)
    {
        var comp = toggleable.Comp;

        comp.Container = _containerSystem.EnsureContainer<Container>(toggleable, comp.ContainerId);
    }

    private void OnAttachedInit(Entity<MultiAttachedClothingComponent> attached, ref ComponentInit args)
    {
        var comp = attached.Comp;

        comp.ClothingContainer = _containerSystem.EnsureContainer<ContainerSlot>(attached, comp.ClothingContainerId);
    }

    /// <summary>
    ///     On map init, either spawn the appropriate entity into the suit slot, or if it already exists, perform some
    ///     sanity checks. Also updates the action icon to show the toggled-entity.
    /// </summary>
    private void OnMapInit(Entity<MultiToggleableClothingComponent> toggleable, ref MapInitEvent args)
    {
        var comp = toggleable.Comp;

        if (comp.Container!.Count != 0)
        {
            DebugTools.Assert(comp.ClothingUids.Count != 0, "Unexpected entity present inside of a toggleable clothing container.");
            return;
        }

        if (comp.ClothingUids.Count != 0 && comp.ActionEntity != null)
            return;

        // Add prototype from ClothingPrototype and Slot field to ClothingPrototypes dictionary
        if (comp.ClothingPrototype != null && !string.IsNullOrEmpty(comp.Slot) && !comp.ClothingPrototypes.ContainsKey(comp.Slot))
        {
            comp.ClothingPrototypes.Add(comp.Slot, comp.ClothingPrototype.Value);
        }

        var xform = Transform(toggleable.Owner);

        if (comp.ClothingPrototypes == null)
            return;

        var prototypes = comp.ClothingPrototypes;

        foreach (var prototype in prototypes)
        {
            var spawned = Spawn(prototype.Value, xform.Coordinates);
            var attachedClothing = EnsureComp<MultiAttachedClothingComponent>(spawned);
            attachedClothing.AttachedUid = toggleable;
            EnsureComp<ContainerManagerComponent>(spawned);

            comp.ClothingUids.Add(spawned, prototype.Key);
            _containerSystem.Insert(spawned, comp.Container, containerXform: xform);

            Dirty(spawned, attachedClothing);
        }

        Dirty(toggleable, comp);

        if (_actionContainer.EnsureAction(toggleable, ref comp.ActionEntity, out var action, comp.Action))
            _actionsSystem.SetEntityIcon(comp.ActionEntity.Value, toggleable);
    }

    // Checks status of all attached clothings toggle status
    public MultiToggleableClothingAttachedStatus GetAttachedToggleStatus(EntityUid toggleable, MultiToggleableClothingComponent? component = null)
    {
        if (!Resolve(toggleable, ref component))
            return MultiToggleableClothingAttachedStatus.NoneToggled;

        var container = component.Container;
        var attachedClothings = component.ClothingUids;

        // If entity don't have any attached clothings it means none toggled
        if (container == null || attachedClothings.Count == 0)
            return MultiToggleableClothingAttachedStatus.NoneToggled;

        var toggledCount = attachedClothings.Count(c => !container.Contains(c.Key));

        if (toggledCount == 0)
            return MultiToggleableClothingAttachedStatus.NoneToggled;

        if (toggledCount < attachedClothings.Count)
            return MultiToggleableClothingAttachedStatus.PartlyToggled;

        return MultiToggleableClothingAttachedStatus.AllToggled;
    }

    public List<EntityUid>? GetAttachedClothingsList(EntityUid toggleable, MultiToggleableClothingComponent? component = null)
    {
        if (!Resolve(toggleable, ref component) || component.ClothingUids.Count == 0)
            return null;

        var newList = new List<EntityUid>();

        foreach (var attachee in component.ClothingUids)
            newList.Add(attachee.Key);

        return newList;
    }
}

public sealed partial class MultiToggleClothingEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class MultiToggleClothingDoAfterEvent : SimpleDoAfterEvent
{
}

// #Misfits Change /Add/: Fired when a toggleable clothing item fully deploys or fully retracts its attached pieces.
public readonly record struct MultiToggleableClothingToggledEvent(EntityUid User, bool Activated);

/// <summary>
///     Event raises on toggleable clothing when someone trying to toggle it
/// </summary>
public sealed class MultiToggleClothingAttemptEvent : CancellableEntityEventArgs
{
    public EntityUid User { get; }
    public EntityUid Target { get; }

    public MultiToggleClothingAttemptEvent(EntityUid user, EntityUid target)
    {
        User = user;
        Target = target;
    }
}

/// <summary>
/// Status of toggleable clothing attachee
/// </summary>
[Serializable, NetSerializable]
public enum MultiToggleableClothingAttachedStatus : byte
{
    NoneToggled,
    PartlyToggled,
    AllToggled
}
