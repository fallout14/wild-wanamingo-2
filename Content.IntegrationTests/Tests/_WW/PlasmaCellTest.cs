using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Pins which cell each plasma gun runs on: pistols on small energy cells, rifles and heavies on microfusion cells.
/// </summary>
[TestFixture]
public sealed class PlasmaCellTest
{
    private const string Small = "F14PowerCellSmall";
    private const string Microfusion = "F14MicrofusionCell";

    [TestCase("F14WeaponPlasmaPistol", Small, Microfusion)]
    [TestCase("F14WeaponPlasmaDefender", Small, Microfusion)]
    [TestCase("F14WeaponPlasmaRifle", Microfusion, Small)]
    [TestCase("F14WeaponPlasmaRifleAuto", Microfusion, Small)]
    [TestCase("F14WeaponPlasmaRifleCrude", Microfusion, Small)]
    [TestCase("F14WeaponPlasmaRifleUrban", Microfusion, Small)]
    [TestCase("F14WeaponPlasmaMultiplas", Microfusion, Small)]
    [TestCase("F14WeaponPlasmaCaster", Microfusion, Small)]
    public async Task PlasmaGunTakesItsCell(string gunId, string accepted, string rejected)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var slots = entMan.System<ItemSlotsSystem>();
            var gun = entMan.SpawnEntity(gunId, map.GridCoords);
            Assert.That(slots.TryGetSlot(gun, "gun_magazine", out var slot), Is.True, $"{gunId} has no cell slot");
            Assert.That(slot!.HasItem, Is.True, $"{gunId} should spawn with a cell");
            Assert.That(slots.TryEject(gun, slot, null, out _), Is.True);

            var good = entMan.SpawnEntity(accepted, map.GridCoords);
            var bad = entMan.SpawnEntity(rejected, map.GridCoords);
            Assert.That(slots.CanInsert(gun, slot, bad, null), Is.False, $"{gunId} should not take {rejected}");
            Assert.That(slots.CanInsert(gun, slot, good, null), Is.True, $"{gunId} should take {accepted}");
        });

        await pair.CleanReturnAsync();
    }
}
