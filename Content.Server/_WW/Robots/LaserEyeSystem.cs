using Content.Server.Chat.Systems;
using Content.Shared._WW.Robots;
using Content.Shared.Chat;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._WW.Robots;

/// <summary>
/// WW: runs the laser eye. Use in hand: charge (emote + whine; the battery fills). Once full, the first shot starts
/// the beam: the robot is rooted and the gun keeps firing, following the shooter's cursor, until the battery runs dry
/// or the beam time is up; then it cools down.
/// </summary>
public sealed partial class LaserEyeSystem : SharedLaserEyeSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LaserEyeComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<LaserEyeComponent, GunShotEvent>(OnShot);
        SubscribeNetworkEvent<LaserEyeAimEvent>(OnAim);
    }

    private void OnUse(Entity<LaserEyeComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var user = args.User;
        var now = _timing.CurTime;

        switch (ent.Comp.State)
        {
            case LaserEyeState.Charging:
                _popup.PopupEntity(Loc.GetString("laser-eye-charging"), user, user);
                return;
            case LaserEyeState.Charged:
                _popup.PopupEntity(Loc.GetString("laser-eye-ready"), user, user);
                return;
            case LaserEyeState.Firing:
                return;
            case LaserEyeState.Cooldown when now < ent.Comp.PhaseEnd:
                var left = (int) Math.Ceiling((ent.Comp.PhaseEnd - now).TotalSeconds);
                _popup.PopupEntity(Loc.GetString("laser-eye-cooling", ("seconds", left)), user, user);
                return;
        }

        SetState(ent, LaserEyeState.Charging, now + ent.Comp.ChargeDuration);
        ent.Comp.Shooter = user;
        Dirty(ent);
        _audio.PlayPvs(ent.Comp.ChargeSound, user);
        Emote(user, ent.Comp.ChargeEmote);
    }

    // The first charged shot turns into the beam.
    private void OnShot(Entity<LaserEyeComponent> ent, ref GunShotEvent args)
    {
        if (ent.Comp.State != LaserEyeState.Charged || !TryComp<GunComponent>(ent, out var gun))
            return;

        SetState(ent, LaserEyeState.Firing, _timing.CurTime + ent.Comp.BeamDuration);
        ent.Comp.Shooter = args.User;
        ent.Comp.Target = gun.ShootCoordinates;
        Dirty(ent);
        EnsureComp<LaserEyeFiringComponent>(args.User);
        _audio.PlayPvs(ent.Comp.BeamSound, args.User);
        Emote(args.User, ent.Comp.FireEmote);
    }

    // The shooter steers the beam with their cursor.
    private void OnAim(LaserEyeAimEvent msg, EntitySessionEventArgs args)
    {
        var gun = GetEntity(msg.Gun);
        if (!TryComp<LaserEyeComponent>(gun, out var eye) || eye.State != LaserEyeState.Firing
            || eye.Shooter != args.SenderSession.AttachedEntity)
        {
            return;
        }

        eye.Target = GetCoordinates(msg.Coordinates);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<LaserEyeComponent, GunComponent, BatteryAmmoProviderComponent>();
        while (query.MoveNext(out var uid, out var eye, out var gun, out var ammo))
        {
            switch (eye.State)
            {
                case LaserEyeState.Charging when now >= eye.PhaseEnd:
                    if (TryComp<BatteryComponent>(uid, out var cell))
                        _battery.SetCharge((uid, cell), cell.MaxCharge); // top it off exactly
                    SetState((uid, eye), LaserEyeState.Charged, now);
                    if (eye.Shooter is { } user)
                        _popup.PopupEntity(Loc.GetString("laser-eye-ready"), user, user, PopupType.Medium);
                    break;

                case LaserEyeState.Firing when now >= eye.PhaseEnd || eye.Shooter is not { } || TerminatingOrDeleted(eye.Shooter)
                                               || _battery.GetCharge((uid, null)) < ammo.FireCost:
                    if (eye.Shooter is { } shooter && !TerminatingOrDeleted(shooter))
                        RemComp<LaserEyeFiringComponent>(shooter);
                    _battery.SetCharge(uid, 0f);
                    eye.Target = null;
                    SetState((uid, eye), LaserEyeState.Cooldown, now + eye.Cooldown);
                    break;

                // Keep the beam going (the gun's fire rate paces it).
                case LaserEyeState.Firing when eye.Target is { } target:
                    _gun.AttemptShoot(eye.Shooter!.Value, (uid, gun), target);
                    break;

                case LaserEyeState.Cooldown when now >= eye.PhaseEnd:
                    SetState((uid, eye), LaserEyeState.Idle, now);
                    break;
            }
        }
    }

    private void SetState(Entity<LaserEyeComponent> ent, LaserEyeState state, TimeSpan phaseEnd)
    {
        ent.Comp.State = state;
        ent.Comp.PhaseEnd = phaseEnd;
        Dirty(ent);
        _battery.RefreshChargeRate(ent.Owner);
    }

    private void Emote(EntityUid user, LocId message)
    {
        _chat.TrySendInGameICMessage(user, Loc.GetString(message), InGameICChatType.Emote, ChatTransmitRange.Normal,
            ignoreActionBlocker: true);
    }
}
