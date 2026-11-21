using Content.Server.Power.Components;
using Content.Shared._WW.Power;
using Content.Shared.Audio;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Server._WW.Power;

/// <summary>
/// Runs <see cref="FusionCoreGeneratorComponent"/>: power out while a charged core is slotted, core charge drained
/// by the load.
/// </summary>
public sealed partial class FusionCoreGeneratorSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAmbientSoundSystem _ambient = default!;

    /// <summary>
    /// Seconds between core drains. Battery charge is networked, so it isn't touched every tick.
    /// </summary>
    private const float DrainInterval = 1f;

    public override void Initialize()
    {
        SubscribeLocalEvent<FusionCoreGeneratorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FusionCoreGeneratorComponent, EntInsertedIntoContainerMessage>(OnSlotChanged);
        SubscribeLocalEvent<FusionCoreGeneratorComponent, EntRemovedFromContainerMessage>(OnSlotChanged);
        SubscribeLocalEvent<FusionCoreGeneratorComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMapInit(Entity<FusionCoreGeneratorComponent> ent, ref MapInitEvent args)
    {
        Refresh(ent);
    }

    private void OnSlotChanged<T>(Entity<FusionCoreGeneratorComponent> ent, ref T args) where T : ContainerModifiedMessage
    {
        if (args.Container.ID == ent.Comp.Slot)
            Refresh(ent);
    }

    private void OnExamined(Entity<FusionCoreGeneratorComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (GetCore(ent) is not { } core)
        {
            args.PushMarkup(Loc.GetString("fusion-core-generator-examine-empty"));
            return;
        }

        var percent = (int) MathF.Round(_battery.GetChargeLevel(core.AsNullable()) * 100f);
        args.PushMarkup(Loc.GetString("fusion-core-generator-examine-core", ("percent", percent)));
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<FusionCoreGeneratorComponent, PowerSupplierComponent>();
        while (query.MoveNext(out var uid, out var gen, out var supplier))
        {
            gen.Accumulator += frameTime;
            if (gen.Accumulator < DrainInterval)
                continue;

            var elapsed = gen.Accumulator;
            gen.Accumulator = 0f;

            if (GetCore((uid, gen)) is { } core && gen.SupplyRate > 0f && supplier.CurrentSupply > 0f)
            {
                var load = Math.Clamp(supplier.CurrentSupply / gen.SupplyRate, 0f, 1f);
                var perSecond = core.Comp.MaxCharge / (float) gen.CoreLifetime.TotalSeconds;
                _battery.UseCharge(core.AsNullable(), perSecond * load * elapsed);
            }

            Refresh((uid, gen), supplier);
        }
    }

    /// <summary>
    /// The slotted core, if it has any charge left.
    /// </summary>
    public Entity<BatteryComponent>? GetCore(Entity<FusionCoreGeneratorComponent> ent)
    {
        if (_slots.GetItemOrNull(ent.Owner, ent.Comp.Slot) is not { } item
            || !TryComp<BatteryComponent>(item, out var battery)
            || _battery.GetCharge((item, battery)) <= 0f)
            return null;

        return (item, battery);
    }

    private void Refresh(Entity<FusionCoreGeneratorComponent> ent, PowerSupplierComponent? supplier = null)
    {
        if (!Resolve(ent, ref supplier, false))
            return;

        var fuelled = GetCore(ent) != null;
        supplier.MaxSupply = fuelled ? ent.Comp.SupplyRate : 0f;
        supplier.Enabled = fuelled;

        var state = !fuelled
            ? FusionCoreGeneratorState.NoCore
            : supplier.CurrentSupply > 0f ? FusionCoreGeneratorState.Running : FusionCoreGeneratorState.Idle;
        _appearance.SetData(ent, FusionCoreGeneratorVisuals.State, state);
        _ambient.SetAmbience(ent, fuelled);
    }
}
