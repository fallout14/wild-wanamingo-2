#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Parallax;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// In mapping mode (a map that hasn't been initialized) an admin ghost doesn't generate planet terrain, so generated
/// rocks and flora can't end up in a saved map. Once the map starts, terrain generates around players as usual.
/// </summary>
[TestOf(typeof(BiomeSystem))]
public sealed class MappingBiomeTest : GameTest
{
    private static readonly ProtoId<BiomeTemplatePrototype> Wasteland = "F14Wasteland";

    [Test]
    public async Task NoTerrainUntilMapInit()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var mapSys = entMan.System<SharedMapSystem>();
        MapId map = default;
        EntityUid mapUid = default;

        await server.WaitPost(() =>
        {
            mapUid = mapSys.CreateMap(out map, runMapInit: false);
            entMan.System<BiomeSystem>().EnsurePlanet(mapUid, server.ProtoMan.Index(Wasteland), 1234);
            var ghost = entMan.SpawnEntity("AdminObserver", new MapCoordinates(0, 0, map));
            server.PlayerMan.SetAttachedEntity(server.PlayerMan.Sessions.Single(), ghost);
        });
        await Pair.RunSeconds(2f);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<BiomeComponent>(mapUid).LoadedChunks, Is.Empty,
                "an admin ghost generated terrain on a pre-init (mapping) map");
            mapSys.InitializeMap(map);
        });
        await Pair.RunSeconds(2f);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<BiomeComponent>(mapUid).LoadedChunks, Is.Not.Empty,
                "a started map generates terrain around the admin ghost");
            entMan.DeleteEntity(mapUid);
        });
    }
}
