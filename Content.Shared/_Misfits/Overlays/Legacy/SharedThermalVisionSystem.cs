// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/Overlays/Switchable/
// SwitchableOverlaySystem.cs, ThermalVisionSystem.cs (Misfits' copy of Goobstation's switchable overlays).
// WW: the generic base is folded into the thermal system, since only thermal vision is ported.

using Content.Shared.Actions;
using Content.Shared.Inventory;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._Misfits.Overlays.Legacy;

public sealed partial class SharedThermalVisionSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThermalVisionComponent, ToggleThermalVisionEvent>(OnToggle);
        SubscribeLocalEvent<ThermalVisionComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ThermalVisionComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ThermalVisionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ThermalVisionComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<ThermalVisionComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ThermalVisionComponent, ComponentHandleState>(OnHandleState);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_net.IsClient)
            ActiveTick(frameTime);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsServer)
            ActiveTick(frameTime);
    }

    private void ActiveTick(float frameTime)
    {
        var query = EntityQueryEnumerator<ThermalVisionComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.PulseTime <= 0f || comp.PulseAccumulator >= comp.PulseTime)
                continue;

            // The accumulator is for visually rendering the pulse strength decaying.
            comp.PulseAccumulator += frameTime;

            // Shut off the pulse when its time is up.
            if (comp.PulseAccumulator < comp.PulseTime)
                continue;

            Toggle(uid, comp, false, false);
            RaiseToggled(uid, uid, comp.IsActive);
            RaiseToggled(uid, Transform(uid).ParentUid, comp.IsActive);
        }
    }

    private void OnGetState(Entity<ThermalVisionComponent> ent, ref ComponentGetState args)
    {
        args.State = new ThermalVisionComponentState
        {
            Color = ent.Comp.Color,
            IsActive = ent.Comp.IsActive,
            ActivateSound = ent.Comp.ActivateSound,
            DeactivateSound = ent.Comp.DeactivateSound,
            ToggleAction = ent.Comp.ToggleAction,
            LightRadius = ent.Comp.LightRadius,
        };
    }

    private void OnHandleState(Entity<ThermalVisionComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not ThermalVisionComponentState state)
            return;

        var comp = ent.Comp;
        comp.Color = state.Color;
        comp.ActivateSound = state.ActivateSound;
        comp.DeactivateSound = state.DeactivateSound;
        comp.LightRadius = state.LightRadius;

        if (comp.ToggleAction != state.ToggleAction)
        {
            _actions.RemoveAction(ent.Owner, comp.ToggleActionEntity);
            comp.ToggleAction = state.ToggleAction;
            if (comp.ToggleAction != null)
                _actions.AddAction(ent, ref comp.ToggleActionEntity, comp.ToggleAction);
        }

        if (comp.IsActive == state.IsActive)
            return;

        comp.IsActive = state.IsActive;
        RaiseToggled(ent, comp.IsEquipment ? Transform(ent).ParentUid : ent.Owner, comp.IsActive);
    }

    private void OnGetItemActions(Entity<ThermalVisionComponent> ent, ref GetItemActionsEvent args)
    {
        if (ent.Comp.IsEquipment && ent.Comp.ToggleAction != null && args.SlotFlags is not SlotFlags.POCKET and not null)
            args.AddAction(ref ent.Comp.ToggleActionEntity, ent.Comp.ToggleAction);
    }

    private void OnShutdown(Entity<ThermalVisionComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.IsEquipment)
            return;

        _actions.RemoveAction(ent.Owner, ent.Comp.ToggleActionEntity);
    }

    private void OnInit(Entity<ThermalVisionComponent> ent, ref ComponentInit args)
    {
        ent.Comp.PulseAccumulator = ent.Comp.PulseTime;
    }

    private void OnMapInit(Entity<ThermalVisionComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp is { IsEquipment: false, ToggleActionEntity: null, ToggleAction: not null })
            _actions.AddAction(ent, ref ent.Comp.ToggleActionEntity, ent.Comp.ToggleAction);
    }

    private void OnToggle(Entity<ThermalVisionComponent> ent, ref ToggleThermalVisionEvent args)
    {
        Toggle(ent, ent.Comp, !ent.Comp.IsActive);
        RaiseToggled(ent, args.Performer, ent.Comp.IsActive);
        args.Handled = true;
    }

    private void Toggle(EntityUid uid, ThermalVisionComponent component, bool activate, bool playSound = true)
    {
        if (playSound && _net.IsClient && _timing.IsFirstTimePredicted)
        {
            _audio.PlayEntity(activate ? component.ActivateSound : component.DeactivateSound,
                Filter.Local(),
                uid,
                false);
        }

        if (component.PulseTime > 0f)
        {
            component.PulseAccumulator = activate ? 0f : component.PulseTime;
            return;
        }

        component.IsActive = activate;
        Dirty(uid, component);
    }

    private void RaiseToggled(EntityUid uid, EntityUid user, bool activated)
    {
        var ev = new SwitchableOverlayToggledEvent(user, activated);
        RaiseLocalEvent(uid, ref ev);
    }
}
