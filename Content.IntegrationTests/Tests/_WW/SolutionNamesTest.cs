#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Chemistry.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Tools.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Fallout items keep the solution names their components look up. The port to Wizden's solution format once dropped
/// them (42 items lost "food", welders had "Welder"), so eating or welding found an empty fallback solution instead.
/// </summary>
[TestOf(typeof(SharedSolutionContainerSystem))]
public sealed class SolutionNamesTest : GameTest
{
    [Test]
    public async Task EdiblesAndWeldersFindTheirSolution()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var map = await Pair.CreateTestMap();
        var compFactory = server.ResolveDependency<IComponentFactory>();

        await server.WaitAssertion(() =>
        {
            var solutions = entMan.System<SharedSolutionContainerSystem>();
            var failures = new List<string>();
            foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (proto.Abstract || !proto.ID.StartsWith("F14"))
                    continue;
                // Wizden's paper is edible through a solution it doesn't have; F14 paper inherits that.
                if (!proto.HasComp<EdibleComponent>(compFactory) && !proto.HasComp<WelderComponent>(compFactory)
                    || proto.HasComp<BadFoodComponent>(compFactory))
                    continue;

                var ent = entMan.SpawnEntity(proto.ID, map.GridCoords);
                if (entMan.TryGetComponent<EdibleComponent>(ent, out var edible))
                    Check(ent, edible.Solution);
                if (entMan.TryGetComponent<WelderComponent>(ent, out var welder))
                    Check(ent, welder.FuelSolutionName);
                entMan.DeleteEntity(ent);
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));

            void Check(EntityUid ent, string name)
            {
                // A solution made by the "Solution" fallback prototype means the item had no solution by that name.
                if (!solutions.TryGetSolution(ent, name, out var sol)
                    || entMan.GetComponent<MetaDataComponent>(sol.Value.Owner).EntityPrototype?.ID == SharedSolutionContainerSystem.DefaultSolution.Id)
                    failures.Add($"{entMan.GetComponent<MetaDataComponent>(ent).EntityPrototype!.ID} has no '{name}' solution");
            }
        });
    }
}
