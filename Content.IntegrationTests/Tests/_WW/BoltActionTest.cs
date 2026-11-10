using Content.Shared._Misfits.Weapons.Ranged;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Magazine guns with manual cycling (AMR, Ratslayer, Varmint) must not throw away a live round when cycled after
/// a shot: the spent casing stays chambered until the user cycles.
/// </summary>
[TestFixture]
public sealed class BoltActionTest
{
    [TestCase("F14WeaponSniper50AntiMateriel")]
    [TestCase("F14WeaponSniperRatslayer")]
    [TestCase("F14WeaponSniper556VarmintRifle")]
    public async Task CyclingAfterShotOnlyEjectsTheCasing(string gunId)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var map = await pair.CreateTestMap();
        EntityUid gun = default, user = default, fired = default;
        var liveBefore = 0;

        await server.WaitAssertion(() =>
        {
            var guns = entMan.System<SharedGunSystem>();
            var manual = entMan.System<ManualActionSystem>();
            user = entMan.SpawnEntity("MobHuman", map.GridCoords);
            gun = entMan.SpawnEntity(gunId, map.GridCoords);
            Assert.That(entMan.GetComponent<ChamberMagazineAmmoProviderComponent>(gun).AutoCycle, Is.False);
            Assert.That(entMan.System<SharedHandsSystem>().TryPickupAnyHand(user, gun), Is.True);
            if (entMan.HasComponent<WieldableComponent>(gun))
                Assert.That(entMan.System<SharedWieldableSystem>().TryWield(gun, user), Is.True);

            // Spawned guns may have an empty chamber or an open bolt; a player cycles once before the first shot.
            if (guns.GetChamberEntity(gun) == null ||
                entMan.GetComponent<ChamberMagazineAmmoProviderComponent>(gun).BoltClosed == false)
                Assert.That(manual.TryCycle(user, gun), Is.True);

            fired = guns.GetChamberEntity(gun)!.Value;
            liveBefore = LiveRounds(entMan, guns, gun);
        });
        // Let the fire-rate cooldown from spawning/racking pass (the AMR fires slowly).
        await server.WaitRunTicks(6 * server.ResolveDependency<IGameTiming>().TickRate);

        await server.WaitAssertion(() =>
        {
            var guns = entMan.System<SharedGunSystem>();
            Assert.That(guns.GetChamberEntity(gun), Is.EqualTo(fired));
            Assert.That(guns.AttemptShoot(user, (gun, entMan.GetComponent<GunComponent>(gun)),
                new EntityCoordinates(map.Grid, 5f, 0.5f)), Is.True, "the gun should fire");
        });
        await server.WaitRunTicks(2);

        await server.WaitAssertion(() =>
        {
            var guns = entMan.System<SharedGunSystem>();
            Assert.That(guns.GetChamberEntity(gun), Is.EqualTo(fired), "the spent casing should stay chambered");
            Assert.That(entMan.GetComponent<CartridgeAmmoComponent>(fired).Spent, Is.True);
            Assert.That(entMan.GetComponent<ManualActionComponent>(gun).NeedsCycle, Is.True);

            Assert.That(entMan.System<ManualActionSystem>().TryCycle(user, gun), Is.True);
            var next = guns.GetChamberEntity(gun);
            Assert.That(next, Is.Not.Null.And.Not.EqualTo(fired), "cycling should chamber the next round");
            Assert.That(entMan.GetComponent<CartridgeAmmoComponent>(next!.Value).Spent, Is.False);
            Assert.That(LiveRounds(entMan, guns, gun), Is.EqualTo(liveBefore - 1), "only the fired round is used up");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task EveryManualMagazineGunIsBoltAction()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var factory = server.ResolveDependency<IComponentFactory>();

        await server.WaitAssertion(() =>
        {
            foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (proto.Abstract || !proto.Components.ContainsKey(factory.GetComponentName<ManualActionComponent>()) ||
                    !proto.TryGetComponent<ChamberMagazineAmmoProviderComponent>(out var chamber, factory))
                {
                    continue;
                }

                Assert.That(chamber.AutoCycle, Is.False,
                    $"{proto.ID} needs cycling, so its ChamberMagazineAmmoProvider needs autoCycle: false");
            }
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Unspent rounds in the chamber plus the magazine.
    /// </summary>
    private static int LiveRounds(IEntityManager entMan, SharedGunSystem guns, EntityUid gun)
    {
        var live = 0;
        if (guns.GetChamberEntity(gun) is { } chambered &&
            entMan.TryGetComponent<CartridgeAmmoComponent>(chambered, out var cartridge) && !cartridge.Spent)
        {
            live++;
        }

        if (entMan.System<SharedContainerSystem>().TryGetContainer(gun, "gun_magazine", out var slot) &&
            slot.ContainedEntities.Count > 0)
        {
            var magazine = slot.ContainedEntities[0];
            var ev = new GetAmmoCountEvent();
            entMan.EventBus.RaiseLocalEvent(magazine, ref ev);
            live += ev.Count;
        }

        return live;
    }
}
