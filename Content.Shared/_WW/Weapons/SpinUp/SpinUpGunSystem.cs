using Content.Shared.Hands;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._WW.Weapons.SpinUp;

/// <inheritdoc cref="SpinUpGunComponent"/>
public sealed partial class SpinUpGunSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpinUpGunComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<SpinUpGunComponent, HeldRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshSpeed);
        SubscribeLocalEvent<SpinUpGunComponent, GotUnequippedHandEvent>(OnUnequippedHand);
    }

    private void OnShotAttempted(Entity<SpinUpGunComponent> ent, ref ShotAttemptedEvent args)
    {
        var now = _timing.CurTime;
        ent.Comp.LastTrigger = now;

        if (!ent.Comp.Spinning)
        {
            ent.Comp.Spinning = true;
            ent.Comp.SpinStart = now;
            ent.Comp.Spinner = args.User;
            _audio.PlayPredicted(ent.Comp.SpinUpSound, ent, args.User);
            _speed.RefreshMovementSpeedModifiers(args.User);
        }

        Dirty(ent);

        if (now < ent.Comp.SpinStart + ent.Comp.SpinUpTime)
            args.Cancel();
    }

    private void OnRefreshSpeed(Entity<SpinUpGunComponent> ent, ref HeldRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        if (ent.Comp.Spinning)
            args.Args.ModifySpeed(ent.Comp.SpinningSpeedModifier);
    }

    private void OnUnequippedHand(Entity<SpinUpGunComponent> ent, ref GotUnequippedHandEvent args)
    {
        StopSpinning(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // The server decides when the barrels stop; the client gets it through the component state.
        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<SpinUpGunComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Spinning && now > comp.LastTrigger + comp.SpinDownDelay)
                StopSpinning((uid, comp));
        }
    }

    private void StopSpinning(Entity<SpinUpGunComponent> ent)
    {
        if (!ent.Comp.Spinning)
            return;

        ent.Comp.Spinning = false;
        var spinner = ent.Comp.Spinner;
        ent.Comp.Spinner = null;
        Dirty(ent);

        if (_net.IsServer)
            _audio.PlayPvs(ent.Comp.SpinDownSound, ent);

        if (spinner is { } user && !TerminatingOrDeleted(user))
            _speed.RefreshMovementSpeedModifiers(user);
    }
}
