// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Weapons/Ranged/ManualActionSystem.cs
// Changed for Wild Wanamingo: cycling is done by using the gun in hand (or the Cycle verb) instead of Misfits'
// cycle-firearm keybind, so no edits to Wizden's input code are needed. Slam-fire is off until a keybind exists.

using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared._Misfits.Weapons.Ranged;

public sealed partial class ManualActionSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedGunSystem _guns = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ManualActionComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<ManualActionComponent, GunShotEvent>(OnShot);
        SubscribeLocalEvent<ManualActionComponent, ExaminedEvent>(OnExamine);
        // Before Wizden's gun system so an uncycled gun cycles instead of doing its default use action.
        SubscribeLocalEvent<ManualActionComponent, UseInHandEvent>(OnUseInHand, before: [typeof(SharedGunSystem)]);
        SubscribeLocalEvent<GunComponent, GetVerbsEvent<Verb>>(OnCycleVerb);
        SubscribeLocalEvent<ManualActionComponent, EntInsertedIntoContainerMessage>(OnInserted); // WW Change
    }

    // WW Change start - a magazine's rounds start out "unspawned" (just a count). The client can't predict a
    // server entity out of that count, so after a quick cycle it saw an empty chamber, clicked, and the shooter
    // never heard the server's shot. Real rounds in the magazine can be predicted into the chamber.
    private void OnInserted(Entity<ManualActionComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == SharedGunSystem.MagazineSlot)
            SpawnMagazineRounds(ent);
    }

    private void SpawnMagazineRounds(EntityUid gun)
    {
        if (!_net.IsServer ||
            !HasComp<ChamberMagazineAmmoProviderComponent>(gun) ||
            !_containers.TryGetContainer(gun, SharedGunSystem.MagazineSlot, out var slot) ||
            slot.ContainedEntities.Count == 0 ||
            !TryComp<BallisticAmmoProviderComponent>(slot.ContainedEntities[0], out var mag) ||
            mag.UnspawnedCount <= 0 || mag.Proto == null)
        {
            return;
        }

        var magEnt = slot.ContainedEntities[0];
        var count = mag.UnspawnedCount;
        _guns.SetBallisticUnspawned((magEnt, mag), 0);
        var coords = Transform(magEnt).Coordinates;
        for (var i = 0; i < count; i++)
        {
            var round = Spawn(mag.Proto.Value, coords);
            if (!_guns.TryBallisticInsert((magEnt, mag), round, null, suppressInsertionSound: true))
                Del(round);
        }
    }
    // WW Change end

    private void OnAttemptShoot(Entity<ManualActionComponent> ent, ref AttemptShootEvent args)
    {
        if (!ent.Comp.NeedsCycle)
            return;

        args.Cancelled = true;
        args.Message = Loc.GetString("gun-manual-action-needs-cycle");
    }

    private void OnShot(Entity<ManualActionComponent> ent, ref GunShotEvent args)
    {
        ent.Comp.NeedsCycle = true;
        Dirty(ent);
        SpawnMagazineRounds(ent); // WW Change - in case the magazine refilled itself
    }

    private void OnUseInHand(Entity<ManualActionComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !ent.Comp.NeedsCycle)
            return;

        args.Handled = TryCycle(args.User, ent);
    }

    private void OnCycleVerb(Entity<GunComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;
        var gun = ent.Owner;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("gun-ballistic-cycle"),
            Act = () => TryCycle(user, gun),
        });
    }

    /// <summary>
    /// Cycles the gun the user is holding in their active hand. Only that gun, so a live round isn't discarded
    /// from a gun in another hand or in storage.
    /// </summary>
    public bool TryCycle(EntityUid user, EntityUid gun)
    {
        if (!_hands.TryGetActiveItem(user, out var held) || held != gun ||
            !HasComp<GunComponent>(gun) ||
            !_blocker.CanInteract(user, gun) || !_blocker.CanUseHeldEntity(user, gun))
        {
            return false;
        }

        TryComp<ManualActionComponent>(gun, out var manual);

        if (TryComp<ChamberMagazineAmmoProviderComponent>(gun, out var chamber))
        {
            // Allow initial chambering after spawning or reloading an empty gun.
            if (manual is { NeedsCycle: false } && chamber.BoltClosed != false && _guns.GetChamberEntity(gun) != null)
                return false;

            if (chamber.CanRack)
                _guns.UseChambered(gun, chamber, user);
            else
                _guns.ToggleBolt(gun, chamber, user);
        }
        else if (manual != null)
        {
            if (!manual.NeedsCycle)
                return false;

            _audio.PlayPredicted(manual.CycleSound, gun, user);
        }
        else
        {
            var ev = new UseInHandEvent(user);
            RaiseLocalEvent(gun, ev);
            return ev.Handled;
        }

        if (manual != null)
        {
            manual.NeedsCycle = false;
            Dirty(gun, manual);
        }

        return true;
    }

    private void OnExamine(Entity<ManualActionComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.NeedsCycle
            ? "gun-manual-action-needs-cycle"
            : "gun-manual-action-examine"));
    }
}
