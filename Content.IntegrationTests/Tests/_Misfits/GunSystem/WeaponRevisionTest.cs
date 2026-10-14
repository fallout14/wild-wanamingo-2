// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.IntegrationTests/Tests/_Misfits/GunSystem/WeaponRevisionTest.cs

using System.Collections.Generic;
using Content.Shared._Misfits.Weapons.Attachments;
using Content.Shared._Misfits.Weapons.Ranged;
using Content.Shared._Misfits.Weapons.Attachments.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Foldable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Containers;

namespace Content.IntegrationTests.Tests._Misfits.GunSystem;

[TestFixture]
public sealed class WeaponRevisionTest
{
    // FullSpriteStates (Misfits' full-magazine sprite override) isn't ported: it needs changes to Wizden's MagazineVisuals.

    [Test]
    public async Task MagazineOverlaysAndBoltStates()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            Fresh = true,
            Destructive = true,
        });
        await pair.Client.WaitAssertion(() =>
        {
            var entMan = pair.Client.EntMan;
            var appearance = entMan.System<AppearanceSystem>();
            foreach (var prototype in new[] { "MisfitsWeaponRifle556Rangemaster", "N14WeaponSniper556Tribal",
                         "N14WeaponSniper556TribalUpgraded", "N14WeaponSniper556VarmintRifle", "MisfitsWeaponShotgunBlowback" })
            {
                var gun = entMan.Spawn(prototype);
                var sprite = entMan.GetComponent<SpriteComponent>(gun);
                var baseLayer = sprite.LayerMapGet(Content.Client.Weapons.Ranged.Components.GunVisualLayers.Base);
                var magLayer = sprite.LayerMapGet(Content.Client.Weapons.Ranged.Components.GunVisualLayers.Mag);
                foreach (var closed in new[] { true, false })
                foreach (var loaded in new[] { true, false })
                foreach (var count in new[] { 0, 12, 25 })
                {
                    var data = new Dictionary<Enum, object>
                    {
                        [AmmoVisuals.BoltClosed] = closed,
                        [AmmoVisuals.MagLoaded] = loaded,
                        [AmmoVisuals.AmmoCount] = count,
                        [AmmoVisuals.AmmoMax] = 25,
                    };
                    foreach (var (key, value) in data)
                        appearance.SetData(gun, key, value);
                    var ev = new AppearanceChangeEvent
                    {
                        Component = entMan.GetComponent<AppearanceComponent>(gun),
                        Sprite = sprite,
                        AppearanceData = data,
                    };
                    entMan.EventBus.RaiseLocalEvent(gun, ref ev);
                    Assert.That(sprite.LayerGetState(baseLayer).ToString(), Is.EqualTo(closed ? "base" : "bolt-open"), prototype);
                    Assert.That(sprite.LayerGetState(magLayer).ToString(), Is.EqualTo("mag-0"), prototype);
                    Assert.That(sprite[magLayer].Visible, Is.EqualTo(loaded), prototype);
                }
                entMan.DeleteEntity(gun);
            }
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ParatrooperStockSizeAndRecoilRestoreWithoutStacking()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var map = await pair.CreateTestMap();
        await server.WaitAssertion(() =>
        {
            var gun = entMan.SpawnEntity("MisfitsWeaponRifle10mmM1Carbine", map.GridCoords);
            var fold = entMan.GetComponent<FoldableComponent>(gun);
            var folding = entMan.System<FoldableSystem>();
            var item = entMan.GetComponent<ItemComponent>(gun);
            var firearm = entMan.GetComponent<GunComponent>(gun);
            var guns = entMan.System<SharedGunSystem>();
            guns.RefreshModifiers((gun, firearm));
            var recoil = firearm.AngleIncreaseModified.Degrees;
            var camera = firearm.CameraRecoilScalarModified;
            var slots = entMan.GetComponent<ItemSlotsComponent>(gun);
            var magazine = slots.Slots["gun_magazine"].Item;

            for (var i = 0; i < 3; i++)
            {
                Assert.That(folding.TrySetFolded(gun, fold, true), Is.True);
                Assert.That(item.Size.Id, Is.EqualTo("Normal"));
                Assert.That(firearm.AngleIncreaseModified.Degrees, Is.EqualTo(recoil * 0.8).Within(0.001));
                Assert.That(firearm.CameraRecoilScalarModified, Is.EqualTo(camera * 0.8).Within(0.001));
                guns.RefreshModifiers((gun, firearm));
                Assert.That(firearm.AngleIncreaseModified.Degrees, Is.EqualTo(recoil * 0.8).Within(0.001));
                Assert.That(folding.TrySetFolded(gun, fold, false), Is.True);
                Assert.That(item.Size.Id, Is.EqualTo("Large"));
                Assert.That(firearm.AngleIncreaseModified.Degrees, Is.EqualTo(recoil).Within(0.001));
                Assert.That(slots.Slots["gun_magazine"].Item, Is.EqualTo(magazine));
            }

            folding.TrySetFolded(gun, fold, true);
            var holder = entMan.SpawnEntity(null, map.GridCoords);
            var containers = entMan.System<SharedContainerSystem>();
            var storage = containers.EnsureContainer<ContainerSlot>(holder, "test-storage");
            Assert.That(containers.Insert(gun, storage), Is.True);
            Assert.That(folding.TrySetFolded(gun, fold, false), Is.False);
            Assert.That(item.Size.Id, Is.EqualTo("Normal"));
        });
        await pair.CleanReturnAsync();
    }

    [TestCase("MisfitsWeaponRifle556Marksman", "N14Magazine556Rifle")]
    [TestCase("MisfitsWeaponRifle556Rangemaster", "N14Magazine556Rifle")]
    [TestCase("MisfitsWeaponRifle556Marksman", "N14LongMagazine556Rifle")]
    [TestCase("N14WeaponSMG10mmPipe", "N14MagazineSMG10mm")]
    [TestCase("N14WeaponPistol10mmPipe", "N14MagazinePistol10mm")]
    [TestCase("MisfitsWeaponRifle10mmM1Carbine", "N14MagazineSMG10mm")]
    [TestCase("MisfitsWeaponRifle10mmM1Carbine", "N14MagazinePistol10mm")]
    [TestCase("MisfitsWeaponRifle10mmSkirmisher", "N14MagazineSMG10mm")]
    [TestCase("MisfitsWeaponRifle10mmSkirmisher", "N14MagazinePistol10mm")]
    [TestCase("N14WeaponSniper556Tribal", "N14Magazine556Rifle")]
    [TestCase("N14WeaponSniper556TribalUpgraded", "N14Magazine556Rifle")]
    public async Task DetachableMagazineCanBeReloadedAndFired(string gunId, string magazineId)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var gun = entMan.SpawnEntity(gunId, map.GridCoords);
            var slots = entMan.System<ItemSlotsSystem>();
            Assert.That(slots.TryEject(gun, "gun_magazine", null, out _), Is.True);
            slots.TryEject(gun, "gun_chamber", null, out _);
            var magazine = entMan.SpawnEntity(magazineId, map.GridCoords);
            Assert.That(slots.TryInsert(gun, "gun_magazine", magazine, null), Is.True);
            Assert.That(slots.TryEject(gun, "gun_magazine", null, out var removed), Is.True);
            Assert.That(removed, Is.EqualTo(magazine));
            Assert.That(slots.TryInsert(gun, "gun_magazine", magazine, null), Is.True);
            var gunSystem = entMan.System<SharedGunSystem>();
            // Racking is done by a person holding the gun (cycle key); the gun cannot rack itself.
            var user = entMan.SpawnEntity("MobHuman", map.GridCoords);
            Assert.That(entMan.System<SharedHandsSystem>().TryPickupAnyHand(user, gun), Is.True);
            Assert.That(entMan.System<ManualActionSystem>().TryCycle(user, gun), Is.True);
            Assert.That(gunSystem.GetChamberEntity(gun), Is.Not.Null, "Cycling after a reload must chamber a round.");
            Assert.That(TakeAmmo(entMan, gun, 1), Has.Count.EqualTo(1));
            Assert.That(entMan.HasComponent<BallisticAmmoProviderComponent>(gun), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task InternalRiflesAndIntegralSuppressor()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var svt = entMan.SpawnEntity("MisfitsWeaponRifle308SVT40", map.GridCoords);
            Assert.That(entMan.GetComponent<MetaDataComponent>(svt).EntityName, Is.EqualTo("SVT-40"));
            var user = entMan.SpawnEntity("MobHuman", map.GridCoords);
            Assert.That(entMan.System<SharedHandsSystem>().TryPickupAnyHand(user, svt), Is.True);
            var optic = entMan.SpawnEntity("MisfitsWeaponScopeVariable", map.GridCoords);
            Assert.That(entMan.System<ItemSlotsSystem>().TryInsert(svt, "weapon_optic", optic, null), Is.True);
            Assert.That(entMan.GetComponent<FirearmAttachmentHostComponent>(svt).OpticToggleActionEntity, Is.Not.Null);
            Assert.That(entMan.HasComponent<ChamberMagazineAmmoProviderComponent>(svt), Is.False);
            Assert.That(TakeAmmo(entMan, svt, 11), Has.Count.EqualTo(10));
            var rifle = entMan.SpawnEntity("MisfitsWeaponRifle9mmMarksmanChinese", map.GridCoords);
            Assert.That(entMan.GetComponent<FirearmAttachmentHostComponent>(rifle).ShowMuzzleVisual, Is.False);
            var suppressed = new IsGunSuppressedEvent();
            entMan.EventBus.RaiseLocalEvent(rifle, ref suppressed);
            Assert.That(suppressed.Suppressed, Is.True);
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Takes ammo from a gun the way firing does (Misfits had this as SharedGunSystem.DoTakeAmmo).
    /// </summary>
    private static List<(EntityUid? Entity, IShootable Shootable)> TakeAmmo(IEntityManager entMan, EntityUid gun, int shots)
    {
        var ev = new TakeAmmoEvent(shots, new List<(EntityUid? Entity, IShootable Shootable)>(),
            entMan.GetComponent<TransformComponent>(gun).Coordinates, null);
        entMan.EventBus.RaiseLocalEvent(gun, ev);
        return ev.Ammo;
    }
}
