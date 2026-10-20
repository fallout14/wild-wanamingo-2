#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Misfits.Construction;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// A flatpack with extras (the Eighties motorbike flatpacks) drops them next to what it unpacks.
/// </summary>
[TestOf(typeof(FlatpackExtrasSystem))]
public sealed class FlatpackExtrasTest : GameTest
{
    private static readonly EntProtoId Flatpack = "F14EightiesGreenMotorbikeFlatpack";
    private static readonly EntProtoId Bike = "F14VehicleBikeGreen";
    private static readonly EntProtoId Key = "F14VehicleKeyMotorbike";
    private static readonly EntProtoId Multitool = "Multitool";
    private static readonly EntProtoId User = "MobHuman";

    [Test]
    public async Task UnpackDropsExtras()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var user = entMan.SpawnEntity(User, map.GridCoords);
            var tool = entMan.SpawnEntity(Multitool, map.GridCoords);
            var flatpack = entMan.SpawnEntity(Flatpack, map.GridCoords);

            entMan.System<SharedInteractionSystem>().InteractUsing(user, tool, flatpack, map.GridCoords,
                checkCanInteract: false, checkCanUse: false);
        });
        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            int Count(string id) => entMan.EntityQuery<MetaDataComponent>()
                .Count(m => m.EntityPrototype?.ID == id);

            Assert.That(Count(Flatpack.Id), Is.Zero, "the flatpack unpacked");
            Assert.That(Count(Bike.Id), Is.EqualTo(1));
            Assert.That(Count(Key.Id), Is.EqualTo(1), "the keys come with the bike");
        });
    }
}
