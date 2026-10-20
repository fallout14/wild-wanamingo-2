#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Server._Forge.Trade;
using Content.Shared._Forge.Trade;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// The faction trade machines (Corvax Forge's NcStore, as Misfits ran it) load their buy/sell catalogs.
/// </summary>
[TestOf(typeof(StoreSystemStructuredLoader))]
public sealed class TradeMachineTest : GameTest
{
    private static readonly EntProtoId[] Machines =
    [
        "F14TrademachineNCRCity",
        "F14TrademachineLegionCity",
        "F14TrademachineWasterCity",
        "F14TrademachineCity",
    ];

    [Test]
    public async Task CatalogsLoad()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var loader = entMan.System<StoreSystemStructuredLoader>();
            foreach (var proto in Machines)
            {
                var machine = entMan.SpawnEntity(proto, map.GridCoords);
                var store = entMan.GetComponent<NcStoreComponent>(machine);
                loader.EnsureLoaded(machine, store, "test");
                Assert.That(store.Listings, Is.Not.Empty, $"{proto} has nothing to buy or sell");
                entMan.DeleteEntity(machine);
            }
        });
    }
}
