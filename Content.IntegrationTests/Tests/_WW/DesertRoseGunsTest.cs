#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared._Misfits.Weapons.Attachments.Components;
using Content.Shared._Misfits.Weapons;
using Content.Shared._Misfits.Weapons.Ranged;
using Content.Shared._WW.Weapons;
using Content.Shared._WW.Weapons.Scopes;
using Content.Shared._Misfits.Special.Components;
using Content.Shared._Misfits.Weapons.TwinTube;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Whitelist;
using Content.Shared.Movement.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Guns built on the 2026-11-14 Desert Rose sprite port: the Berettas, SIG, shotgun revolver, P90, m/45, De Lisle,
/// G11/G11E (4.73mm caseless), AUG, XL70E3, M1919 and M249.
/// </summary>
public sealed class DesertRoseGunsTest : GameTest
{
    private static readonly EntProtoId Cartridge473 = "F14Cartridge473Caseless";
    private static readonly EntProtoId Bullet473 = "F14Bullet473Caseless";
    private static readonly EntProtoId Bullet303 = "F14Bullet303";

    private static readonly string[] Guns =
    {
        "F14WeaponPistol9mmBerettaM92", "F14WeaponPistol9mmBerettaM93R", "F14WeaponPistol9mmSigP220",
        "F14WeaponRevolver20gaJudge", "F14WeaponSMG10mmP90", "F14WeaponSMG9mmCarlGustav", "F14WeaponRifle45Commando",
        "F14WeaponRifle473G11", "F14WeaponRifle473G11E", "F14WeaponRifle556AUG", "F14WeaponRifle556XL70E3",
        "F14WeaponLMG308M1919", "F14WeaponLMG556M249", "F14WeaponHMG50M2Browning",
    };

    [Test]
    public async Task AllSpawnLoaded()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var slots = entMan.System<ItemSlotsSystem>();
            Assert.Multiple(() =>
            {
                foreach (var id in Guns)
                {
                    var gun = entMan.SpawnEntity(id, map.GridCoords);
                    Assert.That(entMan.HasComponent<GunComponent>(gun), id);
                    if (id == "F14WeaponRevolver20gaJudge")
                    {
                        var rev = entMan.GetComponent<RevolverAmmoProviderComponent>(gun);
                        Assert.That(rev.Capacity, Is.EqualTo(5));
                        Assert.That(entMan.HasComponent<CustomSpriteAmmoCounterComponent>(gun), "shell icons, as the other shotguns");
                        continue;
                    }

                    Assert.That(slots.TryGetSlot(gun, "gun_magazine", out var mag) && mag.Item != null, id + " starts with a magazine");
                }
            });
        });
    }

    [Test]
    public async Task Specifics()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var slots = entMan.System<ItemSlotsSystem>();
            EntityUid Spawn(string id) => entMan.SpawnEntity(id, map.GridCoords);
            var whitelist = entMan.System<EntityWhitelistSystem>();
            // Whether the slot accepts the item; a slot that starts loaded (magazine, chamber) is judged by its whitelist.
            bool Takes(EntityUid gun, string slot, string item)
            {
                if (!slots.TryGetSlot(gun, slot, out var s))
                    return false;
                var ent = Spawn(item);
                return s.HasItem
                    ? !s.Locked && whitelist.IsWhitelistPassOrNull(s.Whitelist, ent)
                    : slots.CanInsert(gun, s, ent, null);
            }

            Assert.Multiple(() =>
            {
                // Commando carbine: bolt action, locked integral suppressor, .45 pistol magazines only, +8 damage, scope.
                var delisle = Spawn("F14WeaponRifle45Commando");
                Assert.That(Takes(delisle, FirearmAttachmentHostComponent.DefaultOpticSlot, "F14WeaponScopeReflex"));
                Assert.That(entMan.HasComponent<ManualActionComponent>(delisle));
                Assert.That(entMan.GetComponent<ChamberMagazineAmmoProviderComponent>(delisle).AutoCycle, Is.False);
                Assert.That(slots.TryGetSlot(delisle, FirearmAttachmentHostComponent.DefaultMuzzleSlot, out var muzzle));
                Assert.That(muzzle!.Locked);
                Assert.That(entMan.GetComponent<MetaDataComponent>(muzzle.Item!.Value).EntityPrototype!.ID, Is.EqualTo("F14FirearmSuppressor"));
                Assert.That(Takes(delisle, "gun_magazine", "F14MagazinePistol45"));
                Assert.That(Takes(delisle, "gun_magazine", "F14Magazine45SubMachineGun"), Is.False, "pistol magazines only");
                Assert.That(entMan.GetComponent<GunDamageModifierComponent>(delisle).Damage.DamageDict["Piercing"].Float(), Is.EqualTo(8f));

                // Fixed 4x scopes, no optic slot. The G11s take no muzzle attachments; the AUG and XL70E3 take a
                // suppressor or a bayonet.
                foreach (var id in new[] { "F14WeaponRifle473G11", "F14WeaponRifle473G11E", "F14WeaponRifle556AUG", "F14WeaponRifle556XL70E3" })
                {
                    var gun = Spawn(id);
                    var takesMuzzle = !id.Contains("G11");
                    Assert.That(entMan.GetComponent<WWScopeComponent>(gun).Magnification, Is.EqualTo(4f), id);
                    Assert.That(slots.TryGetSlot(gun, FirearmAttachmentHostComponent.DefaultOpticSlot, out _), Is.False, id);
                    Assert.That(Takes(gun, FirearmAttachmentHostComponent.DefaultMuzzleSlot, "F14FirearmSuppressor"), Is.EqualTo(takesMuzzle), id);
                    Assert.That(Takes(gun, FirearmAttachmentHostComponent.DefaultMuzzleSlot, "F14KnifeBayonet"), Is.EqualTo(takesMuzzle), id);
                }

                // SIG P220: 9mm pistol magazines, one extra damage.
                var sig = Spawn("F14WeaponPistol9mmSigP220");
                Assert.That(Takes(sig, "gun_magazine", "F14MagazinePistol9mm"));
                Assert.That(Takes(sig, "gun_magazine", "F14MagazinePistol45"), Is.False);
                Assert.That(entMan.GetComponent<GunDamageModifierComponent>(sig).Damage.DamageDict["Piercing"].Float(), Is.EqualTo(1f));

                // Canadian rifles: F1 8x and F2 6x built in; the C70 is 5.56 at 7.62 damage and takes a scope.
                Assert.That(entMan.GetComponent<WWScopeComponent>(Spawn("F14WeaponSniper303Canadian")).Magnification, Is.EqualTo(8f));
                Assert.That(entMan.GetComponent<WWScopeComponent>(Spawn("F14WeaponRifle303MarksmanCanadian")).Magnification, Is.EqualTo(6f));
                var c70 = Spawn("F14WeaponRifle762Canadian");
                Assert.That(entMan.HasComponent<WWScopeComponent>(c70), Is.False);
                Assert.That(Takes(c70, FirearmAttachmentHostComponent.DefaultOpticSlot, "F14WeaponScope8x"));
                Assert.That(Takes(c70, "gun_magazine", "F14Magazine556Rifle"));
                Assert.That(Takes(c70, "gun_magazine", "F14Magazine762Rifle"), Is.False);
                Assert.That(entMan.GetComponent<GunDamageModifierComponent>(c70).Damage.DamageDict["Piercing"].Float(), Is.EqualTo(4f));

                // G11E is the better gun.
                var g11 = entMan.GetComponent<GunComponent>(Spawn("F14WeaponRifle473G11"));
                var g11e = entMan.GetComponent<GunComponent>(Spawn("F14WeaponRifle473G11E"));
                Assert.That(g11e.MaxAngle.Degrees, Is.LessThan(g11.MaxAngle.Degrees));
                Assert.That(g11e.BurstFireRate, Is.GreaterThan(g11.BurstFireRate));
                Assert.That(g11.AvailableModes, Is.EqualTo(SelectiveFire.SemiAuto | SelectiveFire.Burst | SelectiveFire.FullAuto));

                // M249: suppressor and scope, no bayonet; takes rifle magazines too.
                var m249 = Spawn("F14WeaponLMG556M249");
                Assert.That(Takes(m249, FirearmAttachmentHostComponent.DefaultMuzzleSlot, "F14FirearmSuppressor"));
                Assert.That(Takes(m249, FirearmAttachmentHostComponent.DefaultMuzzleSlot, "F14KnifeBayonet"), Is.False);
                Assert.That(Takes(m249, FirearmAttachmentHostComponent.DefaultOpticSlot, "F14WeaponScopeAttachment"));
                Assert.That(Takes(m249, "gun_magazine", "F14Magazine556Rifle"));

                // M1919 is .308.
                Assert.That(Takes(Spawn("F14WeaponLMG308M1919"), "gun_chamber", "F14Cartridge308Rifle"));

                // M93R bursts.
                var m93r = entMan.GetComponent<GunComponent>(Spawn("F14WeaponPistol9mmBerettaM93R"));
                Assert.That((m93r.AvailableModes & SelectiveFire.Burst) != 0);
            });
        });
    }

    [Test]
    public async Task CaselessAmmo()
    {
        var protos = Pair.Server.ProtoMan;
        var factory = Pair.Server.EntMan.ComponentFactory;
        await Pair.Server.WaitAssertion(() =>
        {
            Assert.That(protos.Index(Cartridge473).TryGetComponent<CartridgeAmmoComponent>(out var cart, factory));
            Assert.That(cart!.DeleteOnSpawn, Is.True, "caseless: no casing left behind");
            Assert.That(protos.Index(Bullet473).TryGetComponent<ProjectileComponent>(out var proj, factory));
            Assert.That(proj!.ArmorPenetration, Is.EqualTo(0.2f));
        });
    }

    /// <summary>
    /// The Neostead holds 6+6+1. Firing takes the chambered shell first, then the selected tube, and moves to the
    /// other tube by itself once the selected one is empty.
    /// </summary>
    [Test]
    public async Task NeosteadTwinTubes()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var guns = entMan.System<SharedGunSystem>();
            var tubes = entMan.System<TwinTubeAmmoProviderSystem>();
            var gun = entMan.SpawnEntity("F14WeaponShotgunNeostead", map.GridCoords);
            var comp = entMan.GetComponent<TwinTubeAmmoProviderComponent>(gun);
            int Count(EntityUid? part) => part == null ? -1 : guns.GetAmmoCount(part.Value);

            TakeAmmoEvent Take(int shots)
            {
                var ev = new TakeAmmoEvent(shots, new(), map.GridCoords, null);
                entMan.EventBus.RaiseLocalEvent(gun, ev);
                return ev;
            }

            Assert.That(guns.GetAmmoCount(gun), Is.EqualTo(13));
            Assert.That(guns.GetAmmoCapacity(gun), Is.EqualTo(13));
            Assert.That(comp.RightSelected, Is.False);

            Assert.That(Take(1).Ammo, Has.Count.EqualTo(1));
            Assert.That(Count(tubes.GetChamber(gun)), Is.EqualTo(0), "the chamber fires first");
            Assert.That(Count(tubes.GetTube(gun, false)), Is.EqualTo(6));

            Assert.That(Take(6).Ammo, Has.Count.EqualTo(6));
            Assert.That(Count(tubes.GetTube(gun, false)), Is.EqualTo(0));
            Assert.That(comp.RightSelected, Is.True, "an empty tube switches to the other one");

            tubes.SelectTube((gun, comp), false); // switch at will, even to the empty tube
            Assert.That(comp.RightSelected, Is.False);
            Assert.That(Take(6).Ammo, Has.Count.EqualTo(6), "firing an empty tube moves to the loaded one");
            Assert.That(guns.GetAmmoCount(gun), Is.EqualTo(0));
            Assert.That(Take(1).Ammo, Is.Empty);
        });
    }

    /// <summary>
    /// One trigger pull is one burst. The server's burst loop used to reset the shot counter, so the client's next
    /// shoot request started a second burst (the M93R and G11 fired two; the G11E kept going until empty).
    /// </summary>
    [TestCase("F14WeaponPistol9mmBerettaM93R")]
    [TestCase("F14WeaponRifle473G11")]
    [TestCase("F14WeaponRifle473G11E")]
    public async Task OneBurstPerTriggerPull(string id)
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();
        EntityUid gun = default, user = default;
        var before = 0;

        await server.WaitAssertion(() =>
        {
            user = entMan.SpawnEntity("MobHuman", map.GridCoords);
            gun = entMan.SpawnEntity(id, map.GridCoords);
            Assert.That(entMan.System<SharedHandsSystem>().TryPickupAnyHand(user, gun));
            before = entMan.System<SharedGunSystem>().GetAmmoCount(gun);
        });
        await server.WaitRunTicks(server.ResolveDependency<IGameTiming>().TickRate);
        await server.WaitAssertion(() =>
        {
            // Spawned guns may have an open bolt; a player racks once before the first shot.
            if (entMan.GetComponent<ChamberMagazineAmmoProviderComponent>(gun).BoltClosed == false)
                Assert.That(entMan.System<ManualActionSystem>().TryCycle(user, gun), "rack");
            if (entMan.HasComponent<WieldableComponent>(gun))
                Assert.That(entMan.System<SharedWieldableSystem>().TryWield(gun, user), "wield");
        });
        await server.WaitRunTicks(server.ResolveDependency<IGameTiming>().TickRate);

        var target = new EntityCoordinates(map.Grid, 5f, 0.5f);
        await server.WaitAssertion(() =>
        {
            var comp = entMan.GetComponent<GunComponent>(gun);
            // As a non-continuous RequestShootEvent: shoot without resetting the shot counter.
            Assert.That(entMan.System<SharedGunSystem>().ContinueBurst(user, (gun, comp), target), "first shot");
        });

        // The server finishes the burst; meanwhile the client, still holding the trigger, keeps asking to shoot.
        for (var i = 0; i < 40; i++)
        {
            await server.WaitRunTicks(2);
            await server.WaitPost(() =>
            {
                var comp = entMan.GetComponent<GunComponent>(gun);
                entMan.System<SharedGunSystem>().ContinueBurst(user, (gun, comp), target);
            });
        }

        await server.WaitAssertion(() =>
        {
            var comp = entMan.GetComponent<GunComponent>(gun);
            Assert.That(comp.BurstActivated, Is.False);
            Assert.That(before - entMan.System<SharedGunSystem>().GetAmmoCount(gun), Is.EqualTo(3), "exactly one three-round burst");
        });
    }

    [Test]
    public async Task DerringerFitsInPocket()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var inv = entMan.System<InventorySystem>();
            var user = entMan.SpawnEntity("MobHuman", map.GridCoords);
            var suit = entMan.SpawnEntity("ClothingUniformJumpsuitColorGrey", map.GridCoords);
            Assert.That(inv.TryEquip(user, suit, "jumpsuit", silent: true, force: true));
            foreach (var id in new[] { "F14WeaponDerringer", "F14WeaponDerringerGold" })
            {
                var derringer = entMan.SpawnEntity(id, map.GridCoords);
                Assert.That(entMan.GetComponent<ItemComponent>(derringer).Shape, Has.Count.EqualTo(1), id);
                Assert.That(inv.CanEquip(user, derringer, "pocket1", out var reason), reason);
            }
        });
    }

    /// <summary>Scoping in slows you down: least at Perception 10, most at 1.</summary>
    [Test]
    public async Task ScopedSlowdownScalesWithPerception()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var scopes = entMan.System<WWScopeSystem>();
            var user = entMan.SpawnEntity("MobHuman", map.GridCoords);
            var gun = entMan.SpawnEntity("F14WeaponRifle556AUG", map.GridCoords);
            var scope = new Entity<WWScopeComponent>(gun, entMan.GetComponent<WWScopeComponent>(gun));
            var spc = entMan.EnsureComponent<SpecialComponent>(user);

            spc.BasePerception = 1;
            var low = scopes.GetScopedSpeedMultiplier(scope, user);
            spc.BasePerception = 10;
            var high = scopes.GetScopedSpeedMultiplier(scope, user);
            Assert.That(low, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(high, Is.EqualTo(0.9f).Within(0.001f));

            // In use: wield, scope in, and the holder slows down; scope out and the speed comes back.
            Assert.That(entMan.System<SharedHandsSystem>().TryPickupAnyHand(user, gun));
            Assert.That(entMan.System<SharedWieldableSystem>().TryWield(gun, user));
            var move = entMan.GetComponent<MovementSpeedModifierComponent>(user);
            var walk = move.CurrentWalkSpeed;
            scopes.SetActive(scope, true, user);
            Assert.That(move.CurrentWalkSpeed, Is.EqualTo(walk * high).Within(0.01f));
            scopes.SetActive(scope, false, user);
            Assert.That(move.CurrentWalkSpeed, Is.EqualTo(walk).Within(0.01f));
        });
    }

    [Test]
    public async Task Ammo303()
    {
        var protos = Pair.Server.ProtoMan;
        var factory = Pair.Server.EntMan.ComponentFactory;
        await Pair.Server.WaitAssertion(() =>
        {
            Assert.That(protos.Index(Bullet303).TryGetComponent<ProjectileComponent>(out var p303, factory));
            Assert.That(p303!.Damage.DamageDict["Piercing"].Float(), Is.EqualTo(36f));
        });
    }
}
