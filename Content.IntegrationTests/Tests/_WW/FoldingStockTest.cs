#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared._Misfits.Weapons.Attachments.Components;
using Content.Shared._WW.Weapons.Ranged;
using Content.Shared._WW.Weapons.Scopes;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Folding stocks (AC556F, paratrooper carbine): folding shrinks the gun and loosens its spread, extending puts it
/// back, a stowed gun can't be folded. Plus the Mini-14 / AC556F set-up.
/// </summary>
[TestOf(typeof(FoldingStockComponent))]
public sealed class FoldingStockTest : GameTest
{
    [Test]
    public async Task FoldAndExtend()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var fold = entMan.System<FoldingStockSystem>();
            var hands = entMan.System<SharedHandsSystem>();
            var user = entMan.SpawnEntity("MobHuman", map.GridCoords);
            var gun = entMan.SpawnEntity("F14WeaponRifle556AC556F", map.GridCoords);
            var stock = entMan.GetComponent<FoldingStockComponent>(gun);
            var gunComp = entMan.GetComponent<GunComponent>(gun);
            var item = entMan.GetComponent<ItemComponent>(gun);

            Assert.That(stock.Folded, Is.False);
            Assert.That(item.Size.Id, Is.EqualTo("Large"));
            var minAngle = gunComp.MinAngleModified.Degrees;
            var maxAngle = gunComp.MaxAngleModified.Degrees;

            Assert.That(hands.TryPickupAnyHand(user, gun));
            Assert.That(stock.ActionEntity, Is.Not.Null, "holding it gives the fold action");

            Assert.That(fold.TrySetFolded((gun, stock), true, user));
            Assert.Multiple(() =>
            {
                Assert.That(stock.Folded);
                Assert.That(item.Size.Id, Is.EqualTo("Normal"), "folded it fits in a bag");
                Assert.That(gunComp.MinAngleModified.Degrees, Is.EqualTo(minAngle * stock.FoldedSpreadMultiplier).Within(0.01));
                Assert.That(gunComp.MaxAngleModified.Degrees, Is.EqualTo(maxAngle * stock.FoldedSpreadMultiplier).Within(0.01));
            });
            Assert.That(fold.TrySetFolded((gun, stock), true, user), Is.False, "already folded");

            Assert.That(fold.TrySetFolded((gun, stock), false, user));
            Assert.Multiple(() =>
            {
                Assert.That(item.Size.Id, Is.EqualTo("Large"));
                Assert.That(gunComp.MinAngleModified.Degrees, Is.EqualTo(minAngle).Within(0.01));
                Assert.That(gunComp.MaxAngleModified.Degrees, Is.EqualTo(maxAngle).Within(0.01));
            });
        });
    }

    [Test]
    public async Task StowedCantFold()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var gun = entMan.SpawnEntity("F14WeaponRifle10mmM1Carbine", map.GridCoords);
            var box = entMan.SpawnEntity(null, map.GridCoords);
            var containers = entMan.System<SharedContainerSystem>();
            var bag = containers.EnsureContainer<Container>(box, "test-bag");
            Assert.That(containers.Insert(gun, bag));

            var stock = entMan.GetComponent<FoldingStockComponent>(gun);
            Assert.That(entMan.System<FoldingStockSystem>().TrySetFolded((gun, stock), true), Is.False);
            Assert.That(stock.Folded, Is.False);
        });
    }

    [Test]
    public async Task Mini14AndAC556F()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var slots = entMan.System<ItemSlotsSystem>();
            var whitelist = entMan.System<EntityWhitelistSystem>();
            bool Takes(EntityUid gun, string slot, string item)
            {
                if (!slots.TryGetSlot(gun, slot, out var s))
                    return false;
                var ent = entMan.SpawnEntity(item, map.GridCoords);
                return s.HasItem
                    ? !s.Locked && whitelist.IsWhitelistPassOrNull(s.Whitelist, ent)
                    : slots.CanInsert(gun, s, ent, null);
            }

            var mini = entMan.SpawnEntity("F14WeaponRifle556Mini14", map.GridCoords);
            var ac = entMan.SpawnEntity("F14WeaponRifle556AC556F", map.GridCoords);
            Assert.Multiple(() =>
            {
                foreach (var gun in new[] { mini, ac })
                {
                    Assert.That(slots.TryGetSlot(gun, "gun_magazine", out var mag) && mag.Item != null, "starts loaded");
                    Assert.That(Takes(gun, "gun_magazine", "F14LongMagazine556Rifle"));
                    Assert.That(Takes(gun, FirearmAttachmentHostComponent.DefaultMuzzleSlot, "F14FirearmSuppressor"));
                }

                const string muzzle = FirearmAttachmentHostComponent.DefaultMuzzleSlot;
                Assert.That(Takes(mini, muzzle, "F14KnifeBayonet"));
                Assert.That(Takes(mini, muzzle, "F14FirearmRecoilCompensator"));
                Assert.That(Takes(ac, muzzle, "F14KnifeBayonet"), Is.False, "the AC556F takes a suppressor only");
                Assert.That(Takes(ac, muzzle, "F14FirearmRecoilCompensator"), Is.False, "the AC556F takes a suppressor only");

                var miniGun = entMan.GetComponent<GunComponent>(mini);
                Assert.That(miniGun.AvailableModes, Is.EqualTo(SelectiveFire.SemiAuto));
                Assert.That(entMan.HasComponent<FoldingStockComponent>(mini), Is.False);
                Assert.That(entMan.GetComponent<FirearmAttachmentHostComponent>(mini).EnableOpticSlot, "the Mini-14 takes a scope");

                var acGun = entMan.GetComponent<GunComponent>(ac);
                Assert.That(acGun.AvailableModes, Is.EqualTo(SelectiveFire.SemiAuto | SelectiveFire.Burst | SelectiveFire.FullAuto));
                Assert.That(acGun.ShotsPerBurst, Is.EqualTo(3));
                Assert.That(entMan.GetComponent<WWScopeComponent>(ac).Magnifications, Is.EqualTo(new[] { 1.5f }));
                Assert.That(entMan.GetComponent<FirearmAttachmentHostComponent>(ac).EnableOpticSlot, Is.False, "the reflex is built in");
                Assert.That(entMan.HasComponent<FoldingStockComponent>(ac));
            });
        });
    }
}
