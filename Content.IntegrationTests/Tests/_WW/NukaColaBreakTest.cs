#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// A Nuka-Cola bottle breaks into one broken bottle (2026-11-21 playtest: it made two), plus a bottle cap only if
/// it was never opened.
/// </summary>
public sealed class NukaColaBreakTest : GameTest
{
    private static readonly ProtoId<DamageTypePrototype> Blunt = "Blunt";

    [TestCase("F14DrinkNukaCola", false, 1)]
    [TestCase("F14DrinkNukaCola", true, 0)]
    [TestCase("F14DrinkNukaColaCherry", false, 1)]
    [TestCase("F14DrinkNukaColaQuantum", false, 1)]
    public async Task BreaksIntoOneBottle(string id, bool open, int caps)
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        int Count(string proto) =>
            entMan.AllEntities<MetaDataComponent>().Count(e => e.Comp.EntityPrototype?.ID == proto);

        var capsBefore = 0;
        await server.WaitPost(() =>
        {
            var bottle = entMan.SpawnEntity(id, map.GridCoords);
            if (open)
                entMan.System<OpenableSystem>().TryOpen(bottle);
            capsBefore = Count("F14CurrencyCap");
            var blunt = server.ProtoMan.Index(Blunt);
            entMan.System<DamageableSystem>().TryChangeDamage(bottle, new DamageSpecifier(blunt, 20), ignoreResistances: true);
        });
        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            var broken = Count("F14DrinkNukaColaBroken") + Count("BrokenBottle");
            Assert.That(broken, Is.EqualTo(1), $"{id}: should break into one bottle (Nuka {Count("F14DrinkNukaColaBroken")}, generic {Count("BrokenBottle")})");
            Assert.That(Count("F14CurrencyCap") - capsBefore, Is.EqualTo(caps), $"{id}: wrong number of caps");
        });
    }
}
