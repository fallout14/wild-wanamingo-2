using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared._WW.Power;

/// <summary>
/// Shows a battery as empty (its <see cref="BatteryVisuals.State"/> appearance) once its charge drops to
/// <see cref="Threshold"/> of its maximum. Wizden only calls a battery empty at exactly zero, but a gun stops firing
/// with less than a shot's worth left, so a spent fusion core kept its full sprite. Also sets the state on spawn, so a
/// core that starts empty looks empty (2026-11-22 playtest).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BatterySpentVisualsComponent : Component
{
    /// <summary>
    /// Fraction of max charge at or below which the battery looks empty.
    /// </summary>
    [DataField]
    public float Threshold = 0.01f;
}

public sealed partial class BatterySpentVisualsSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedBatterySystem _battery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BatterySpentVisualsComponent, MapInitEvent>(OnMapInit,
            after: [typeof(SharedBatterySystem)]);
        SubscribeLocalEvent<BatterySpentVisualsComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<BatterySpentVisualsComponent, BatteryStateChangedEvent>(OnStateChanged,
            after: [typeof(SharedBatterySystem)]);
    }

    private void OnMapInit(Entity<BatterySpentVisualsComponent> ent, ref MapInitEvent args)
    {
        if (TryComp<BatteryComponent>(ent, out var battery))
            UpdateAppearance(ent, _battery.GetCharge((ent, battery)), battery.MaxCharge, battery.State);
    }

    private void OnChargeChanged(Entity<BatterySpentVisualsComponent> ent, ref ChargeChangedEvent args)
    {
        if (TryComp<BatteryComponent>(ent, out var battery))
            UpdateAppearance(ent, args.CurrentCharge, args.MaxCharge, battery.State);
    }

    private void OnStateChanged(Entity<BatterySpentVisualsComponent> ent, ref BatteryStateChangedEvent args)
    {
        // Wizden sets the appearance to the new state; keep it "empty" if we're still below the threshold.
        if (TryComp<BatteryComponent>(ent, out var battery))
            UpdateAppearance(ent, _battery.GetCharge((ent, battery)), battery.MaxCharge, args.NewState);
    }

    private void UpdateAppearance(Entity<BatterySpentVisualsComponent> ent, float charge, float max, BatteryState state)
    {
        if (max > 0f && charge <= max * ent.Comp.Threshold)
            state = BatteryState.Empty;
        else if (state == BatteryState.Empty)
            state = charge >= max ? BatteryState.Full : BatteryState.Neither;

        _appearance.SetData(ent.Owner, BatteryVisuals.State, state);
    }
}
