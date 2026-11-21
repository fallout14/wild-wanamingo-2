using System.Linq;
using Content.Server.Power.Components;
using Content.Shared._WW.Power;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Server.GameObjects;

namespace Content.Server._WW.Power;

/// <summary>
/// Fills the RobCo power terminal's screen with the power devices around it, once a second while it's open.
/// </summary>
public sealed partial class RobcoPowerTerminalSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _xforms = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private FusionCoreGeneratorSystem _fusion = default!;

    private const float UpdateInterval = 1f;
    private const LookupFlags Flags = LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Approximate;

    private readonly HashSet<Entity<PowerSupplierComponent>> _suppliers = new();
    private readonly HashSet<Entity<PowerNetworkBatteryComponent>> _batteries = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RobcoPowerTerminalComponent, BoundUIOpenedEvent>(OnOpened);
    }

    private void OnOpened(Entity<RobcoPowerTerminalComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RobcoPowerTerminalComponent>();
        while (query.MoveNext(out var uid, out var terminal))
        {
            terminal.Accumulator += frameTime;
            if (terminal.Accumulator < UpdateInterval)
                continue;

            terminal.Accumulator = 0f;
            if (_ui.IsUiOpen(uid, RobcoPowerTerminalUiKey.Key))
                UpdateUi((uid, terminal));
        }
    }

    private void UpdateUi(Entity<RobcoPowerTerminalComponent> ent)
    {
        _ui.SetUiState(ent.Owner, RobcoPowerTerminalUiKey.Key, new RobcoPowerTerminalState(GetDevices(ent), ent.Comp.Range));
    }

    /// <summary>
    /// The anchored power devices in range, nearest first.
    /// </summary>
    public List<RobcoPowerDevice> GetDevices(Entity<RobcoPowerTerminalComponent> ent)
    {
        var devices = new List<RobcoPowerDevice>();
        var xform = Transform(ent);
        var origin = _xforms.GetMapCoordinates(ent.Owner, xform);

        _suppliers.Clear();
        _lookup.GetEntitiesInRange(xform.Coordinates, ent.Comp.Range, _suppliers, Flags);
        foreach (var (uid, supplier) in _suppliers)
        {
            if (!InRange(uid, origin, ent.Comp.Range, out var device))
                continue;

            device.Kind = RobcoPowerDeviceKind.Generator;
            device.Output = supplier.CurrentSupply;
            device.Capacity = supplier.Enabled ? supplier.MaxSupply : 0f;
            if (TryComp<FusionCoreGeneratorComponent>(uid, out var fusion))
                device.Fuel = _fusion.GetCore((uid, fusion)) is { } core ? _battery.GetChargeLevel(core.AsNullable()) : 0f;

            devices.Add(device);
        }

        _batteries.Clear();
        _lookup.GetEntitiesInRange(xform.Coordinates, ent.Comp.Range, _batteries, Flags);
        foreach (var (uid, netBattery) in _batteries)
        {
            // Breakers (APCs) discharge through their Apc component; substations and storage through a discharger.
            RobcoPowerDeviceKind kind;
            if (HasComp<ApcComponent>(uid))
                kind = RobcoPowerDeviceKind.Breaker;
            else if (TryComp<BatteryDischargerComponent>(uid, out var discharger))
                kind = discharger.Voltage == Voltage.Medium ? RobcoPowerDeviceKind.Substation : RobcoPowerDeviceKind.Storage;
            else
                continue;

            if (!InRange(uid, origin, ent.Comp.Range, out var device))
                continue;

            device.Kind = kind;
            device.Output = netBattery.CurrentSupply;
            device.Capacity = netBattery.MaxSupply;
            device.Input = netBattery.CurrentReceiving;
            if (TryComp<BatteryComponent>(uid, out var battery))
                device.Charge = _battery.GetChargeLevel((uid, battery));

            devices.Add(device);
        }

        return devices.OrderBy(d => d.Kind).ThenBy(d => d.Distance).ThenBy(d => d.Name).ToList();
    }

    private bool InRange(EntityUid uid, Robust.Shared.Map.MapCoordinates origin, float range, out RobcoPowerDevice device)
    {
        device = default!;
        var xform = Transform(uid);
        if (!xform.Anchored)
            return false;

        var pos = _xforms.GetMapCoordinates(uid, xform);
        var offset = pos.Position - origin.Position;
        if (pos.MapId != origin.MapId || offset.Length() > range)
            return false;

        device = new RobcoPowerDevice
        {
            Name = Name(uid),
            Distance = (int) MathF.Round(offset.Length()),
            Direction = offset.LengthSquared() < 0.25f ? Direction.Invalid : offset.ToWorldAngle().GetDir(),
        };
        return true;
    }
}
