#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Server._Misfits.Crafting;
using Content.Server.Lathe;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// A blueprint kept in a workbench's storage adds its recipes to the bench; taking it out removes them.
/// </summary>
[TestOf(typeof(WorkbenchBlueprintsSystem))]
public sealed class WorkbenchBlueprintsTest : GameTest
{
    private static readonly EntProtoId Bench = "N14WorkbenchWeaponbench";
    private static readonly EntProtoId Blueprint = "MisfitsBlueprintEightiesWeaponsT1";
    private static readonly ProtoId<LatheRecipePrototype> PipePistol = "MisfitsBlueprintPipePistolEighties";

    [Test]
    public async Task StoredBlueprintAddsRecipes()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var lathe = entMan.System<LatheSystem>();
            var bench = entMan.SpawnEntity(Bench, map.GridCoords);
            var latheComp = entMan.GetComponent<LatheComponent>(bench);
            Assert.That(lathe.GetAvailableRecipes(bench, latheComp, getUnavailable: true), Does.Not.Contain(PipePistol),
                "no blueprint, no blueprint recipe");

            var blueprint = entMan.SpawnEntity(Blueprint, map.GridCoords);
            Assert.That(entMan.System<SharedStorageSystem>().Insert(bench, blueprint, out _, playSound: false));
            Assert.That(lathe.GetAvailableRecipes(bench, latheComp, getUnavailable: true), Does.Contain(PipePistol),
                "the stored blueprint's recipes are on the bench");

            entMan.System<SharedContainerSystem>().TryRemoveFromContainer(blueprint);
            Assert.That(lathe.GetAvailableRecipes(bench, latheComp, getUnavailable: true), Does.Not.Contain(PipePistol),
                "taking the blueprint out removes them");

            entMan.DeleteEntity(bench);
            entMan.DeleteEntity(blueprint);
        });
    }
}
