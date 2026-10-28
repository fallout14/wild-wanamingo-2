#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared._Misfits.Weapons.Ranged;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids.Components;
using Content.Shared.Temperature.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Problems found in the first in-game pass on the Dev map (2026-10-26).
/// </summary>
public sealed class PlaytestFixesTest : GameTest
{
    [Test]
    public async Task PlaytestFixesHold()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var solutions = entMan.System<SharedSolutionContainerSystem>();

            // Toilets: draw water from the bowl, and what's dumped in drains away.
            foreach (var proto in new[] { "F14ToiletWater", "F14ToiletDirty", "F14ToiletIrradiated" })
            {
                var toilet = entMan.SpawnEntity(proto, map.GridCoords);
                Assert.That(entMan.TryGetComponent<DrainableSolutionComponent>(toilet, out var drainable), proto);
                Assert.That(solutions.TryGetSolution(toilet, drainable!.Solution, out _, out var water) && water.Volume > 0, proto);
                Assert.That(entMan.TryGetComponent<DumpableSolutionComponent>(toilet, out var dumpable), proto);
                Assert.That(solutions.TryGetSolution(toilet, dumpable!.Solution, out _), proto);
                Assert.That(entMan.HasComponent<DrainComponent>(toilet), proto);
            }

            // Stoves and grills heat on their own; their setting must not draw grid power.
            foreach (var proto in new[] { "F14CookingStove", "F14CookingStoveWide", "F14KitchenGrill", "F14KitchenGrillWide" })
            {
                var heater = entMan.SpawnEntity(proto, map.GridCoords);
                Assert.That(entMan.GetComponent<EntityHeaterComponent>(heater).Power, Is.Zero, proto);
            }

            // Single-shot breechloaders don't need racking.
            foreach (var proto in new[] { "F14WeaponSniper50NCRRifle", "F14WeaponSniper50Pipe" })
            {
                Assert.That(entMan.HasComponent<ManualActionComponent>(entMan.SpawnEntity(proto, map.GridCoords)), Is.False, proto);
            }

            // Stack icons follow the count only with Appearance.
            foreach (var proto in new[] { "F14Suture", "F14ImprovisedSuture", "F14AloeMesh", "F14AloeSalve" })
            {
                Assert.That(entMan.HasComponent<AppearanceComponent>(entMan.SpawnEntity(proto, map.GridCoords)), proto);
            }
        });
    }
}
