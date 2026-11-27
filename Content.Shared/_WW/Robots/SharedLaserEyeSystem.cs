using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;
using Content.Shared.Power;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._WW.Robots;

/// <summary>
/// WW: the predicted half of the laser eye: it won't fire unless charged, its battery fills while charging, and a
/// firing robot can't move. The charge and beam run on the server (LaserEyeSystem).
/// </summary>
public abstract partial class SharedLaserEyeSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LaserEyeComponent, ShotAttemptedEvent>(OnShotAttempt);
        SubscribeLocalEvent<LaserEyeComponent, RefreshChargeRateEvent>(OnRefreshChargeRate);
        SubscribeLocalEvent<LaserEyeFiringComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<LaserEyeFiringComponent, ComponentStartup>(OnFiringChanged);
        SubscribeLocalEvent<LaserEyeFiringComponent, ComponentShutdown>(OnFiringChanged);
    }

    private void OnShotAttempt(Entity<LaserEyeComponent> ent, ref ShotAttemptedEvent args)
    {
        if (ent.Comp.State is not (LaserEyeState.Charged or LaserEyeState.Firing))
            args.Cancel();
    }

    // Charging fills the whole battery over the charge-up time; otherwise it holds its charge.
    private void OnRefreshChargeRate(Entity<LaserEyeComponent> ent, ref RefreshChargeRateEvent args)
    {
        if (ent.Comp.State == LaserEyeState.Charging)
            args.NewChargeRate += args.MaxCharge / (float) ent.Comp.ChargeDuration.TotalSeconds;
    }

    private void OnUpdateCanMove(Entity<LaserEyeFiringComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.LifeStage <= ComponentLifeStage.Running)
            args.Cancel();
    }

    private void OnFiringChanged<T>(Entity<LaserEyeFiringComponent> ent, ref T args)
    {
        _blocker.UpdateCanMove(ent);
    }
}
