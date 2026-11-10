using System.Linq;
using Content.Shared._WW.Roofing;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Construction.Steps;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Players roof a tile from the construction menu for 1 wood plank, and take the roof off with a saw.
/// </summary>
[TestFixture]
public sealed class RoofTest
{
    private static readonly ProtoId<ConstructionPrototype> Recipe = "WWRoofWood";

    [Test]
    public async Task RoofRecipeCostsOnePlank()
    {
        await using var pair = await PoolManager.GetServerClient();
        var protos = pair.Server.ResolveDependency<IPrototypeManager>();

        var recipe = protos.Index(Recipe);
        var graph = protos.Index<ConstructionGraphPrototype>(recipe.Graph);
        var steps = graph.Nodes[recipe.StartNode].GetEdge(recipe.TargetNode)!.Steps;
        Assert.That(steps, Has.Count.EqualTo(1));
        var step = (MaterialConstructionGraphStep) steps[0];
        Assert.That(step.MaterialPrototypeId.Id, Is.EqualTo("WoodPlank"));
        Assert.That(step.Amount, Is.EqualTo(1));
        Assert.That(recipe.Conditions.OfType<TileRoofed>().Single().Roofed, Is.False);

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task BuildThenSawOffRoof()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var map = await pair.CreateTestMap();
        Entity<MapGridComponent, RoofComponent> grid = default;
        var tile = map.Tile.GridIndices;
        EntityUid user = default, roofEnt = default;
        var openSky = new TileRoofed { Roofed = false };
        var roofed = new TileRoofed { Roofed = true };

        await server.WaitAssertion(() =>
        {
            // No roof tracking on this grid yet: can't build.
            Assert.That(openSky.Condition(EntityUid.Invalid, map.GridCoords, Direction.South), Is.False);

            var roof = entMan.EnsureComponent<RoofComponent>(map.Grid);
            grid = (map.Grid.Owner, map.Grid.Comp, roof);
            Assert.That(openSky.Condition(EntityUid.Invalid, map.GridCoords, Direction.South), Is.True);
            Assert.That(roofed.Condition(EntityUid.Invalid, map.GridCoords, Direction.South), Is.False);

            // What the construction spawns when finished.
            roofEnt = entMan.SpawnEntity("WWRoofWood", map.GridCoords);
        });
        await server.WaitRunTicks(2);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.Deleted(roofEnt), Is.True, "the roof entity should delete itself");
            Assert.That(entMan.System<SharedRoofSystem>().IsRooved(grid, tile), Is.True);
            Assert.That(openSky.Condition(EntityUid.Invalid, map.GridCoords, Direction.South), Is.False,
                "can't roof a tile twice");
            Assert.That(roofed.Condition(EntityUid.Invalid, map.GridCoords, Direction.South), Is.True);

            user = entMan.SpawnEntity("MobHuman", map.GridCoords);
            var saw = entMan.SpawnEntity("F14Saw", map.GridCoords);
            Assert.That(entMan.System<SharedHandsSystem>().TryPickupAnyHand(user, saw), Is.True);
            entMan.System<SharedInteractionSystem>().UserInteraction(user, map.GridCoords, null);
        });
        await server.WaitRunTicks(6 * server.ResolveDependency<IGameTiming>().TickRate);

        await server.WaitAssertion(() =>
        {
            Assert.That(RoofRemoverSystem.HasRoofFlag(grid.Comp2, tile), Is.False, "the saw should take the roof off");
            Assert.That(entMan.System<SharedRoofSystem>().IsRooved(grid, tile), Is.False);
        });

        await pair.CleanReturnAsync();
    }
}
