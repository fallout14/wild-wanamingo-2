// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.IntegrationTests/Tests/_Misfits/GunSystem/RifleCaliberTest.cs

using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Misfits.GunSystem;

[TestFixture]
public sealed class RifleCaliberTest
{
    [TestCase("F14Magazine308Rifle", "F14Cartridge762x51FMJ")]
    [TestCase("F14Magazine308Rifle", "F14Cartridge762x51AP")]
    [TestCase("F14Magazine308Rifle", "F14Cartridge308JHP")]
    [TestCase("F14Magazine308Rifle", "F14Cartridge308Match")]
    [TestCase("F14Magazine556Rifle", "F14Cartridge556FMJ")]
    [TestCase("F14Magazine556Rifle", "F14Cartridge556AP")]
    [TestCase("F14Magazine556Rifle", "F14Cartridge223JHP")]
    public async Task SharedCalibersFitExistingMagazines(string magazineId, string cartridgeId)
    {
        await using var pair = await PoolManager.GetServerClient();
        var map = await pair.CreateTestMap();
        await pair.Server.WaitAssertion(() =>
        {
            var em = pair.Server.ResolveDependency<IEntityManager>();
            var magazine = em.SpawnEntity(magazineId, map.GridCoords);
            var cartridge = em.SpawnEntity(cartridgeId, map.GridCoords);
            var provider = em.GetComponent<BallisticAmmoProviderComponent>(magazine);
            Assert.That(em.System<EntityWhitelistSystem>().IsWhitelistPass(provider.Whitelist, cartridge), Is.True);
            var wrongRound = em.SpawnEntity("F14Cartridge303", map.GridCoords);
            Assert.That(em.System<EntityWhitelistSystem>().IsWhitelistPass(provider.Whitelist, wrongRound), Is.False);
        });
        await pair.CleanReturnAsync();
    }

    [TestCase("F14WeaponSniperEnfield")]
    [TestCase("F14WeaponSniper303Ross")]
    [TestCase("F14Magazine303Bren")]
    [TestCase("F14SpeedLoader303")]
    public async Task BritishProvidersLoadOnly303(string prototype)
    {
        await using var pair = await PoolManager.GetServerClient();
        var map = await pair.CreateTestMap();
        await pair.Server.WaitAssertion(() =>
        {
            var em = pair.Server.ResolveDependency<IEntityManager>();
            var entity = em.SpawnEntity(prototype, map.GridCoords);
            var provider = em.GetComponent<BallisticAmmoProviderComponent>(entity);
            Assert.That(provider.Proto?.Id, Is.EqualTo("F14Cartridge303"));
            var whitelist = em.System<EntityWhitelistSystem>();
            var correct = em.SpawnEntity("F14Cartridge303", map.GridCoords);
            var incorrect = em.SpawnEntity("F14Cartridge762x51FMJ", map.GridCoords);
            Assert.That(whitelist.IsWhitelistPass(provider.Whitelist, correct), Is.True);
            Assert.That(whitelist.IsWhitelistPass(provider.Whitelist, incorrect), Is.False);
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task BrenUsesDedicatedMagazineAndChamber()
    {
        await using var pair = await PoolManager.GetServerClient();
        var map = await pair.CreateTestMap();
        await pair.Server.WaitAssertion(() =>
        {
            var em = pair.Server.ResolveDependency<IEntityManager>();
            var weapon = em.SpawnEntity("F14WeaponLMGBren", map.GridCoords);
            var slots = em.System<ItemSlotsSystem>();
            Assert.That(slots.TryEject(weapon, "gun_magazine", null, out var loaded), Is.True);
            Assert.That(em.GetComponent<MetaDataComponent>(loaded!.Value).EntityPrototype!.ID, Is.EqualTo("F14Magazine303Bren"));
            Assert.That(slots.TryInsert(weapon, "gun_magazine", loaded.Value, null), Is.True);
            Assert.That(slots.TryEject(weapon, "gun_magazine", null, out _), Is.True);
            var wrongMagazine = em.SpawnEntity("F14Magazine308RifleLong", map.GridCoords);
            Assert.That(slots.TryInsert(weapon, "gun_magazine", wrongMagazine, null), Is.False);
            Assert.That(slots.TryEject(weapon, "gun_chamber", null, out var chambered), Is.True);
            Assert.That(em.GetComponent<MetaDataComponent>(chambered!.Value).EntityPrototype!.ID, Is.EqualTo("F14Cartridge303"));
        });
        await pair.CleanReturnAsync();
    }
}
