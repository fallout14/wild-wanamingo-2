#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Lathe;
using Content.Shared._WW.Recipes;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Kitchen;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Recipes on a disabledRecipes list disappear from the crafting menu, workbenches and cooking.
/// </summary>
[TestOf(typeof(DisabledRecipesSystem))]
public sealed class DisabledRecipesTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: disabledRecipes
  id: F14TestDisabledRecipes
  construction: [ Chair ]
  lathe: [ Wirecutter ]
  microwave: [ RecipeBun ]
";

    private static readonly ProtoId<ConstructionPrototype> Chair = "Chair";
    private static readonly ProtoId<ConstructionPrototype> ChairWood = "ChairWood";
    private static readonly EntProtoId Autolathe = "Autolathe";
    private static readonly ProtoId<LatheRecipePrototype> Wirecutter = "Wirecutter";

    [Test]
    public async Task ListedRecipesAreGone()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            Assert.That(protoMan.Index(Chair).Hide, "a listed crafting recipe is hidden");
            Assert.That(protoMan.Index(ChairWood).Hide, Is.False, "others stay");

            Assert.That(entMan.System<RecipeSystem>().Recipes.Any(r => r.ID == "RecipeBun"), Is.False);
            Assert.That(entMan.System<RecipeSystem>().Recipes, Is.Not.Empty);

            var lathe = entMan.SpawnEntity(Autolathe, map.GridCoords);
            var recipes = entMan.System<LatheSystem>()
                .GetAvailableRecipes(lathe, entMan.GetComponent<LatheComponent>(lathe), getUnavailable: true);
            Assert.That(recipes, Does.Not.Contain(Wirecutter));
            Assert.That(recipes, Is.Not.Empty);
            entMan.DeleteEntity(lathe);
        });
    }
}
