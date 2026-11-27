using Content.Shared._WW.Stealth;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server._WW.Stealth;

/// <summary>
/// WW: Stealth Boys. A one-use one cloaks the user for a while and becomes its spent version straight away; the
/// Assaultron's rechargeable one toggles the cloak and runs off its battery. Cloaking is the Chinese stealth suit's
/// Stealth setup. See <see cref="StealthBoyComponent"/>.
/// </summary>
public sealed partial class StealthBoySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedStealthSystem _stealth = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedBatterySystem _battery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StealthBoyComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<StealthBoyComponent, RefreshChargeRateEvent>(OnRefreshChargeRate);
        SubscribeLocalEvent<StealthBoyCloakedComponent, ComponentShutdown>(OnCloakShutdown);
    }

    private void OnUse(Entity<StealthBoyComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var user = args.User;

        if (ent.Comp.UsedPrototype is not { } used)
        {
            ToggleRechargeable(ent, user);
            return;
        }

        if (HasComp<StealthBoyCloakedComponent>(user))
        {
            _popup.PopupEntity(Loc.GetString("stealth-boy-already-active"), user, user);
            return;
        }

        Cloak(ent, user).EndTime = _timing.CurTime + ent.Comp.Duration;

        // One use: swap it for the spent one, in the same hand if possible. Not deleted inside the event; sent to
        // nullspace so the hand is free (as SpawnItemsOnUse does).
        _hands.IsHolding(user, ent.Owner, out var hand);
        var spent = Spawn(used, _transform.GetMapCoordinates(user));
        _transform.DetachEntity(ent.Owner, Transform(ent.Owner));
        QueueDel(ent.Owner);
        if (!_hands.TryPickup(user, spent, hand))
            _hands.TryPickupAnyHand(user, spent);
    }

    private void ToggleRechargeable(Entity<StealthBoyComponent> ent, EntityUid user)
    {
        if (ent.Comp.Active)
        {
            RemComp<StealthBoyCloakedComponent>(user); // its shutdown switches the device off
            return;
        }

        if (HasComp<StealthBoyCloakedComponent>(user))
        {
            _popup.PopupEntity(Loc.GetString("stealth-boy-already-active"), user, user);
            return;
        }

        if (_battery.GetCharge((ent.Owner, null)) < ent.Comp.MinCharge)
        {
            _popup.PopupEntity(Loc.GetString("stealth-boy-recharging"), user, user);
            return;
        }

        var cloak = Cloak(ent, user);
        cloak.EndTime = TimeSpan.MaxValue;
        cloak.Source = ent.Owner;
        SetActive(ent, true);
    }

    private StealthBoyCloakedComponent Cloak(Entity<StealthBoyComponent> ent, EntityUid user)
    {
        var cloak = EnsureComp<StealthBoyCloakedComponent>(user);
        cloak.DeactivateSound = ent.Comp.DeactivateSound;
        cloak.AddedStealth = !HasComp<StealthComponent>(user);
        if (cloak.AddedStealth)
        {
            // Nothing else moves its visibility, so it stays at this level until the cloak ends.
            var stealth = AddComp<StealthComponent>(user);
            _stealth.SetVisibility(user, ent.Comp.Visibility, stealth);
        }

        _audio.PlayPvs(ent.Comp.ActivateSound, user);
        _popup.PopupEntity(Loc.GetString("stealth-boy-activated", ("item", ent.Owner)), user, user);
        return cloak;
    }

    private void SetActive(Entity<StealthBoyComponent> ent, bool active)
    {
        ent.Comp.Active = active;
        Dirty(ent);
        _battery.RefreshChargeRate(ent.Owner);
    }

    // The rechargeable one drains while on and refills while off.
    private void OnRefreshChargeRate(Entity<StealthBoyComponent> ent, ref RefreshChargeRateEvent args)
    {
        if (ent.Comp.UsedPrototype != null)
            return;

        args.NewChargeRate += ent.Comp.Active ? -ent.Comp.DrainRate : ent.Comp.RegenRate;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<StealthBoyCloakedComponent>();
        while (query.MoveNext(out var uid, out var cloak))
        {
            if (cloak.Source is not { } source)
            {
                if (now >= cloak.EndTime)
                    RemCompDeferred<StealthBoyCloakedComponent>(uid);
                continue;
            }

            // Rechargeable: ends when it's switched off, runs dry or isn't in their hands any more.
            if (TerminatingOrDeleted(source)
                || !TryComp<StealthBoyComponent>(source, out var boy) || !boy.Active
                || _battery.GetCharge((source, null)) <= 0f
                || !_hands.IsHolding(uid, source))
            {
                RemCompDeferred<StealthBoyCloakedComponent>(uid);
            }
        }
    }

    private void OnCloakShutdown(Entity<StealthBoyCloakedComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Source is { } source && !TerminatingOrDeleted(source) && TryComp<StealthBoyComponent>(source, out var boy))
            SetActive((source, boy), false);

        if (TerminatingOrDeleted(ent))
            return;

        if (ent.Comp.AddedStealth)
            RemComp<StealthComponent>(ent);

        _audio.PlayPvs(ent.Comp.DeactivateSound, ent);
        _popup.PopupEntity(Loc.GetString("stealth-boy-deactivated"), ent, ent);
    }
}
