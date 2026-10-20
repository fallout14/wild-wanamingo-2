// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.IntegrationTests/Tests/_Misfits/GunSystem/WeaponAttachmentRevisionTest.cs

using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Misfits.GunSystem;

[TestFixture]
public sealed class WeaponAttachmentRevisionTest
{
    [TestCase("F14WeaponRifle762SKS")]
    [TestCase("F14WeaponRifle308SVT40")]
    [TestCase("F14WeaponRifle556Service")]
    [TestCase("F14WeaponRifle5mmCarbine")]
    [TestCase("F14WeaponRifle308Battle")]
    [TestCase("F14WeaponRifle308M14")]
    [TestCase("F14WeaponSniperHunting")]
    [TestCase("F14WeaponSniperEnfield")]
    [TestCase("F14WeaponSniper303Ross")]
    [TestCase("F14WeaponRevolver44Magnum")]
    [TestCase("F14WeaponRevolver4570Hunter")]
    public async Task OptionalScopeCanBeInstalled(string prototype)
    {
        await using var pair = await PoolManager.GetServerClient();
        var map = await pair.CreateTestMap();
        await pair.Server.WaitAssertion(() =>
        {
            var em = pair.Server.ResolveDependency<IEntityManager>();
            var gun = em.SpawnEntity(prototype, map.GridCoords);
            var scope = em.SpawnEntity("F14WeaponScopeAttachment", map.GridCoords);
            Assert.That(em.System<ItemSlotsSystem>().TryInsert(gun, "weapon_optic", scope, null), Is.True);
            Assert.That(em.HasComponent<WieldableComponent>(gun), Is.True,
                "The attached scope requires its host to be wieldable.");
        });
        await pair.CleanReturnAsync();
    }

    [TestCase("F14WeaponPistol9mm")]
    [TestCase("F14WeaponSMG45GreaseGun")]
    [TestCase("F14WeaponSMG12mmAdvancedChinese")]
    [TestCase("F14WeaponSMG12mmAdvanced")]
    [TestCase("F14WeaponPistol45Colt")]
    [TestCase("F14WeaponRifle308M14")]
    [TestCase("F14WeaponSMG10mm")]
    [TestCase("F14WeaponPistol10mm")]
    [TestCase("F14WeaponPistol12mm")]
    public async Task SuppressorCanBeInstalled(string prototype)
    {
        await using var pair = await PoolManager.GetServerClient();
        var map = await pair.CreateTestMap();
        await pair.Server.WaitAssertion(() =>
        {
            var em = pair.Server.ResolveDependency<IEntityManager>();
            var gun = em.SpawnEntity(prototype, map.GridCoords);
            var suppressor = em.SpawnEntity("F14FirearmSuppressor", map.GridCoords);
            Assert.That(em.System<ItemSlotsSystem>().TryInsert(gun, "weapon_muzzle", suppressor, null), Is.True);
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ThatGunUsesOnlyFiveRevolverChambersAndBothCalibers()
    {
        await using var pair = await PoolManager.GetServerClient();
        var map = await pair.CreateTestMap();
        await pair.Server.WaitAssertion(() =>
        {
            var em = pair.Server.ResolveDependency<IEntityManager>();
            var gun = em.SpawnEntity("F14WeaponPistol223", map.GridCoords);
            var provider = em.GetComponent<RevolverAmmoProviderComponent>(gun);
            Assert.That(provider.Capacity, Is.EqualTo(5));
            Assert.That(em.HasComponent<ChamberMagazineAmmoProviderComponent>(gun), Is.False);
            Assert.That(em.HasComponent<MagazineAmmoProviderComponent>(gun), Is.False);
            var whitelist = em.System<EntityWhitelistSystem>();
            foreach (var cartridge in new[] { "F14Cartridge556Rifle", "F14Cartridge223JHP", "F14Cartridge556FMJ", "F14Cartridge556AP" })
            {
                var round = em.SpawnEntity(cartridge, map.GridCoords);
                Assert.That(whitelist.IsWhitelistPass(provider.Whitelist, round), Is.True, cartridge);
            }
        });
        await pair.CleanReturnAsync();
    }

    [TestCase("F14WeaponShotgunRiot", "F14MagazineShotgun12", "F14MagazineShotgun20")]
    [TestCase("F14WeaponRifle556EM2", "F14LongMagazine556Rifle", "F14Magazine303EM2")]
    [TestCase("F14WeaponSMG45GreaseGun", "F14Magazine45SubMachineGun", "F14MagazineSMG9mm")]
    public async Task RechamberedWeaponsRejectOldMagazines(string prototype, string correct, string wrong)
    {
        await using var pair = await PoolManager.GetServerClient();
        var map = await pair.CreateTestMap();
        await pair.Server.WaitAssertion(() =>
        {
            var em = pair.Server.ResolveDependency<IEntityManager>();
            var gun = em.SpawnEntity(prototype, map.GridCoords);
            var slots = em.System<ItemSlotsSystem>();
            Assert.That(slots.TryEject(gun, "gun_magazine", null, out _), Is.True);
            var bad = em.SpawnEntity(wrong, map.GridCoords);
            Assert.That(slots.TryInsert(gun, "gun_magazine", bad, null), Is.False);
            var good = em.SpawnEntity(correct, map.GridCoords);
            Assert.That(slots.TryInsert(gun, "gun_magazine", good, null), Is.True);
        });
        await pair.CleanReturnAsync();
    }

    [TestCase("F14WeaponRifle308Battle", true, false)]
    [TestCase("F14WeaponRifle308M14", true, true)]
    [TestCase("F14WeaponRifle308SVT40", true, false)]
    [TestCase("F14WeaponRifle762SKS", true, false)]
    [TestCase("F14WeaponRifle5mmCarbine", false, true)]
    [TestCase("F14WeaponSniperEnfield", true, false)]
    [TestCase("F14WeaponSniper303Ross", true, false)]
    [TestCase("F14WeaponRifle556Service", true, false)]
    public async Task MuzzleRestrictionsAllowScopeAlongsideAttachment(string prototype, bool bayonet, bool suppressor)
    {
        await using var pair = await PoolManager.GetServerClient();
        var map = await pair.CreateTestMap();
        await pair.Server.WaitAssertion(() =>
        {
            var em = pair.Server.ResolveDependency<IEntityManager>();
            var slots = em.System<ItemSlotsSystem>();
            foreach (var (attachment, allowed) in new[] { ("F14FirearmBayonet", bayonet), ("F14FirearmSuppressor", suppressor) })
            {
                var gun = em.SpawnEntity(prototype, map.GridCoords);
                var scope = em.SpawnEntity("F14WeaponScopeAttachment", map.GridCoords);
                Assert.That(slots.TryInsert(gun, "weapon_optic", scope, null), Is.True);
                var item = em.SpawnEntity(attachment, map.GridCoords);
                Assert.That(slots.TryInsert(gun, "weapon_muzzle", item, null), Is.EqualTo(allowed), attachment);
            }
        });
        await pair.CleanReturnAsync();
    }
}
