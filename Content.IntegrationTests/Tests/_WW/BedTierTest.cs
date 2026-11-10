using Content.Shared.Bed.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Fallout beds heal in tiers (rough &lt; frame &lt; proper &lt; medical) and heal bullet and cut wounds, not only
/// Wizden's blunt and poison.
/// </summary>
[TestFixture]
public sealed class BedTierTest
{
    [TestCase("F14Bedroll", 0.04)]
    [TestCase("F14BedrollFolded", 0.04)]
    [TestCase("F14SleepingBagRed", 0.04)]
    [TestCase("F14DirtyMattress", 0.04)]
    [TestCase("F14BedMattressDirty3", 0.04)]
    [TestCase("F14BedDirty", 0.07)]
    [TestCase("F14BedCot", 0.07)]
    [TestCase("F14BedMetalBed", 0.07)]
    [TestCase("F14BedMattress0", 0.07)]
    [TestCase("F14Bed", 0.1)]
    [TestCase("F14BedBunk", 0.1)]
    [TestCase("F14BedWoodBed", 0.1)]
    [TestCase("F14BedMedical", 0.2)]
    public async Task BedHealsAtItsTier(string bedId, double rate)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var bed = entMan.SpawnEntity(bedId, map.GridCoords);
            var heal = entMan.GetComponent<HealOnBuckleComponent>(bed).Damage.DamageDict;
            foreach (var type in new[] { "Blunt", "Slash", "Piercing", "Poison" })
            {
                Assert.That(heal.TryGetValue(type, out var value), Is.True, $"{bedId} does not heal {type}");
                Assert.That(value.Double(), Is.EqualTo(-rate).Within(0.0001), $"{bedId} {type}");
            }
        });

        await pair.CleanReturnAsync();
    }
}
