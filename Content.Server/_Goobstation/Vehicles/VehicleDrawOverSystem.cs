// Ported from Goobstation content carried by Misfits (https://github.com/Misfit-Sanctuary/nuclear-14):
// the overlay and engine-sound parts of Content.Shared/_Goobstation/Vehicles/SharedVehicleSystem.cs
// WW Change - driven by Wizden's vehicle events instead of Goob's own riding code.

using System.Numerics;
using Content.Shared._Goobstation.Vehicles;
using Content.Shared.Audio;
using Content.Shared.Movement.Components;
using Content.Shared.Vehicle;

namespace Content.Server._Goobstation.Vehicles;

/// <summary>
/// Spawns a vehicle's overlay (drawn over its rider) while it's ridden, and runs its engine sound while it can run.
/// </summary>
public sealed partial class VehicleDrawOverSystem : EntitySystem
{
    [Dependency] private SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VehicleDrawOverComponent, VehicleOperatorSetEvent>(OnOperatorSet);
        SubscribeLocalEvent<VehicleDrawOverComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // The engine runs while the vehicle can (key in, fuelled...), as with Goob's engine.
        var query = EntityQueryEnumerator<VehicleDrawOverComponent, InputMoverComponent, AmbientSoundComponent>();
        while (query.MoveNext(out var uid, out _, out var mover, out var ambient))
        {
            if (ambient.Enabled != mover.CanMove)
                _ambient.SetAmbience(uid, mover.CanMove, ambient);
        }
    }

    private void OnOperatorSet(Entity<VehicleDrawOverComponent> ent, ref VehicleOperatorSetEvent args)
    {
        RemoveOverlay(ent);

        if (args.NewOperator == null || ent.Comp.OverlayPrototype is not { } proto)
            return;

        var overlay = SpawnAttachedTo(proto, Transform(ent).Coordinates);
        _transform.SetParent(overlay, ent);
        _transform.SetLocalPositionRotation(overlay, Vector2.Zero, Angle.Zero);
        ent.Comp.ActiveOverlay = overlay;
    }

    private void OnShutdown(Entity<VehicleDrawOverComponent> ent, ref ComponentShutdown args)
    {
        RemoveOverlay(ent);
    }

    private void RemoveOverlay(Entity<VehicleDrawOverComponent> ent)
    {
        if (ent.Comp.ActiveOverlay is { } overlay)
            QueueDel(overlay);

        ent.Comp.ActiveOverlay = null;
    }
}
