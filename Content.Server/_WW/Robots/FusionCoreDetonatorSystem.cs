using Content.Shared._WW.Robots;
using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._WW.Robots;

/// <summary>
/// WW: a Sentry Bot blowing its own fusion core. Starts the chassis' self-destruct fuse (the beeping 8 seconds it
/// also runs on death). See <see cref="FusionCoreDetonatorComponent"/>.
/// </summary>
public sealed partial class FusionCoreDetonatorSystem : EntitySystem
{
    private static readonly ProtoId<TagPrototype> FusionCoreTag = "F14FusionCore";

    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private TriggerSystem _trigger = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FusionCoreDetonatorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FusionCoreDetonatorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<FusionCoreDetonatorComponent, DetonateFusionCoreEvent>(OnDetonate);
    }

    private void OnMapInit(Entity<FusionCoreDetonatorComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnShutdown(Entity<FusionCoreDetonatorComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnDetonate(Entity<FusionCoreDetonatorComponent> ent, ref DetonateFusionCoreEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        if (!_slots.TryGetSlot(ent.Owner, ent.Comp.Slot, out var slot) || slot.Item is not { } core || !_tag.HasTag(core, FusionCoreTag))
        {
            _popup.PopupEntity(Loc.GetString("fusion-core-detonate-no-core"), ent, ent);
            return;
        }

        _popup.PopupEntity(Loc.GetString("fusion-core-detonate-started"), ent, PopupType.LargeCaution);
        _trigger.Trigger(ent, ent, ent.Comp.TriggerKey);
    }
}
