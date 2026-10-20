#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared._Misfits.Nutrition.Legacy;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Opening a Nuka-Cola gives a bottle cap, once.
/// </summary>
[TestOf(typeof(SpawnWhenOpenedComponent))]
public sealed class SpawnWhenOpenedTest : GameTest
{
    [Test]
    public async Task OpeningGivesOneCap()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();
        EntityUid bottle = default;

        await server.WaitPost(() =>
        {
            bottle = entMan.SpawnEntity("F14DrinkNukaCola", map.GridCoords);
            var openable = entMan.System<OpenableSystem>();
            Assert.That(openable.TryOpen(bottle), "the bottle should open");
            openable.TryClose(bottle);
            openable.TryOpen(bottle);
        });
        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            var caps = entMan.AllEntities<MetaDataComponent>().Count(e => e.Comp.EntityPrototype?.ID == "F14CurrencyCap");
            Assert.That(caps, Is.EqualTo(1), "opening (even twice) should give exactly one cap");
        });
    }
}
