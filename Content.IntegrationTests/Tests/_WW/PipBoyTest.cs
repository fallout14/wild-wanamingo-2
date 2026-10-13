#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Pip-Boys work as PDAs with Wizden's programs preinstalled, and draw the Mojave Sun 13 wrist sprite.
/// </summary>
public sealed class PipBoyTest : GameTest
{
    [TestCase("N14Pipboy")]
    [TestCase("N14NCRCommanderPipboy")]
    [TestCase("N14NCRVeteranRangerPipboy")]
    [TestCase("N14HeadPaladin_PipBoy")]
    public async Task PipBoyIsAPda(string proto)
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var pipBoy = entMan.SpawnEntity(proto, map.GridCoords);
            Assert.That(entMan.HasComponent<PdaComponent>(pipBoy), "a Pip-Boy is a PDA");

            var programs = entMan.System<SharedContainerSystem>()
                .GetContainer(pipBoy, CartridgeLoaderComponent.UnremovableContainerId).ContainedEntities
                .Select(e => entMan.GetComponent<MetaDataComponent>(e).EntityPrototype?.ID).ToList();
            Assert.That(programs, Does.Contain("CrewManifestCartridge"));
            Assert.That(programs, Does.Contain("NotekeeperCartridge"));

            var sprite = server.ProtoMan.Index<Robust.Shared.Prototypes.EntityPrototype>(proto);
            Assert.That(sprite.Components.TryGetValue("Clothing", out var clothing));
            entMan.DeleteEntity(pipBoy);
        });
    }
}
