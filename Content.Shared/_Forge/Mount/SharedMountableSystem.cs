// Ported from Corvax Forge content carried by Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_NC/Mount/SharedMountableSystem.cs

using Content.Shared._Forge.Mountable.Components;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.NPC;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee.Events;


namespace Content.Shared._Forge.Mountable;

/// <summary>
/// The system responsible for mount management
/// </summary>
public sealed partial class SharedMountSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        // WW: the rider sits at the strap's buckleOffset (set from riderOffset by the porter kit). Moving the rider
        // per facing made Wizden's buckle system unbuckle them, and Wizden's strap offset can't be changed from here.
        SubscribeLocalEvent<RiderComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MountableComponent, MeleeHitEvent>(OnMeleeHit);

        SubscribeLocalEvent<MountableComponent, DownAttemptEvent>(OnDownAttempt);
        SubscribeLocalEvent<MountableComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<MountableComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<MountableComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMeleeHit(Entity<MountableComponent> ent, ref MeleeHitEvent args)
    {
        if (args.User == ent.Comp.Rider) // Don't hit your own horse
            args.Handled = true;
    }

    /// <summary>
    /// Checking that you can't drop a mount with a rider
    /// </summary>
    private void OnDownAttempt(Entity<MountableComponent> ent, ref DownAttemptEvent args)
    {
        if (ent.Comp.Rider != null)
            args.Cancel();
    }

    /// <summary>
    /// The method of attaching a rider to a mount
    /// </summary>
    private void OnStrapped(Entity<MountableComponent> ent, ref StrappedEvent args)
    {
        if (_mobState.IsDead(ent))
        {
            _buckle.TryUnbuckle(args.Buckle.Owner, ent.Owner);
            return;
        }

        EnsureComp<InputMoverComponent>(ent);
        var rider = EnsureComp<RiderComponent>(args.Buckle.Owner);
        rider.Mount = ent.Owner;
        ent.Comp.Rider = args.Buckle.Owner;

        Dirty(ent.Owner, ent.Comp);
        Dirty(args.Buckle.Owner, rider);

        var controlAttempt = new MountMovementControlAttemptEvent(args.Buckle.Owner);
        RaiseLocalEvent(ent.Owner, ref controlAttempt);
        ent.Comp.RiderControlsMovement = ent.Comp.ControlMovement && !controlAttempt.Cancelled;
        ent.Comp.HadActiveNpcBeforeMount = HasComp<ActiveNPCComponent>(ent);

        if (ent.Comp.RiderControlsMovement)
        {
            _mover.SetRelay(args.Buckle.Owner, ent.Owner);
            RemComp<ActiveNPCComponent>(ent);
        }

        if (_standing.IsDown(ent.Owner))
            _standing.Stand(ent.Owner);

        if (!TryComp<MovementSpeedModifierComponent>(ent, out var move))
            return;

        if (!ent.Comp.RiderControlsMovement)
            return;

        var walk = move.BaseWalkSpeed * ent.Comp.MountedSpeed;
        var sprint = move.BaseSprintSpeed * ent.Comp.MountedSpeed;
        _movement.ChangeBaseSpeed(ent, walk, sprint, move.Acceleration, move);
    }

    /// <summary>
    /// Unties and removes the rider
    /// </summary>
    private void OnUnstrapped(Entity<MountableComponent> ent, ref UnstrappedEvent args)
    {
        if (TryComp<RelayInputMoverComponent>(args.Buckle.Owner, out var relay) &&
            relay.RelayEntity == ent.Owner)
        {
            RemComp<RelayInputMoverComponent>(args.Buckle.Owner);
        }

        RemComp<RiderComponent>(args.Buckle.Owner);
        ent.Comp.Rider = null;
        var riderControlledMovement = ent.Comp.RiderControlsMovement;
        ent.Comp.RiderControlsMovement = false;

        Dirty(ent.Owner, ent.Comp);

        if (ent.Comp.HadActiveNpcBeforeMount)
            EnsureComp<ActiveNPCComponent>(ent);

        ent.Comp.HadActiveNpcBeforeMount = false;
        if (!TryComp<MovementSpeedModifierComponent>(ent, out var move))
            return;

        if (!riderControlledMovement)
            return;

        var walk = move.BaseWalkSpeed / ent.Comp.DefaultSpeed;
        var sprint = move.BaseSprintSpeed / ent.Comp.DefaultSpeed;
        _movement.ChangeBaseSpeed(ent, walk, sprint, move.Acceleration, move);
    }

    /// <summary>
    /// Resets the rider if he starts to die
    /// </summary>
    private void OnMobStateChanged(Entity<RiderComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive || ent.Comp.Mount == null)
            return;

        _buckle.TryUnbuckle(ent.Owner, ent.Comp.Mount);
    }

    /// <summary>
    /// Resets the rider if the mount starts to die and returns everything as it was if everything is fine with it
    /// </summary>
    private void OnMobStateChanged(Entity<MountableComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
        {
            ent.Comp.ControlMovement = false;
            if (ent.Comp.Rider != null)
                _buckle.TryUnbuckle(ent.Comp.Rider.Value, ent.Owner);
        }
        else
        {
            ent.Comp.ControlMovement = true;
            if (ent.Comp.Rider != null)
                _buckle.TryUnbuckle(ent.Comp.Rider.Value, ent.Owner);
        }
    }
}

[ByRefEvent]
public record struct MountMovementControlAttemptEvent(EntityUid Rider)
{
    public bool Cancelled;
}
