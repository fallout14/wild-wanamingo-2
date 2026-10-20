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
    private static readonly EntProtoId PipBoy = "F14Pipboy";
    private static readonly EntProtoId IdCard = "F14IDCardVault";
    private static readonly EntProtoId Terminal = "F14ComputerTerminal";

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
            Assert.That(programs, Does.Contain("F14NanoChatCartridge"));
            Assert.That(programs, Does.Contain("F14PipBoyContactsCartridge"));
            Assert.That(programs, Does.Contain("F14PipBoyGroupsCartridge"));
            Assert.That(programs, Does.Contain("F14PipBoyDirectoryCartridge"));
            Assert.That(programs, Does.Contain("F14PipBoySettingsCartridge"));

            var card = entMan.SpawnEntity(IdCard, map.GridCoords);
            Assert.That(entMan.GetComponent<NanoChatCardComponent>(card).Number, Is.Not.Null, "ID cards get a NanoChat number");

            var terminal = entMan.SpawnEntity(Terminal, map.GridCoords);
            Assert.That(entMan.HasComponent<TerminalNotebookComponent>(terminal), "terminals keep notes");
            Assert.That(entMan.HasComponent<TerminalDatabaseAccessComponent>(terminal), "terminals open the faction database");
        });
    }
}
