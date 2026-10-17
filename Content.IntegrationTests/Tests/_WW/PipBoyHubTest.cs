#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared._DeltaV.NanoChat;
using Content.Shared._Misfits.Holotape;
using Content.Shared.CartridgeLoader;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Pip-Boys carry Misfits' hub programs and NanoChat; ID cards get a NanoChat number; terminals have notes and
/// the faction database.
/// </summary>
public sealed class PipBoyHubTest : GameTest
{
    private static readonly EntProtoId PipBoy = "N14Pipboy";
    private static readonly EntProtoId IdCard = "N14IDCardVault";
    private static readonly EntProtoId Terminal = "N14ComputerTerminal";

    [Test]
    public async Task PipBoyHasTheHubAndNanoChat()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var pipBoy = entMan.SpawnEntity(PipBoy, map.GridCoords);
            var programs = entMan.System<SharedContainerSystem>()
                .GetContainer(pipBoy, CartridgeLoaderComponent.UnremovableContainerId).ContainedEntities
                .Select(e => entMan.GetComponent<MetaDataComponent>(e).EntityPrototype?.ID).ToList();
            Assert.That(programs, Does.Contain("DeltaVNanoChatCartridge"));
            Assert.That(programs, Does.Contain("MisfitsPipBoyContactsCartridge"));
            Assert.That(programs, Does.Contain("MisfitsPipBoyGroupsCartridge"));
            Assert.That(programs, Does.Contain("MisfitsPipBoyDirectoryCartridge"));
            Assert.That(programs, Does.Contain("MisfitsPipBoySettingsCartridge"));

            var card = entMan.SpawnEntity(IdCard, map.GridCoords);
            Assert.That(entMan.GetComponent<NanoChatCardComponent>(card).Number, Is.Not.Null, "ID cards get a NanoChat number");

            var terminal = entMan.SpawnEntity(Terminal, map.GridCoords);
            Assert.That(entMan.HasComponent<TerminalNotebookComponent>(terminal), "terminals keep notes");
            Assert.That(entMan.HasComponent<TerminalDatabaseAccessComponent>(terminal), "terminals open the faction database");
        });
    }
}
