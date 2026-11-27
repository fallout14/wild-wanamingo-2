#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Decals;
using Content.Server.Parallax;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// The F14Wasteland biome lays MS13 sand everywhere around a player, with Fallout wild plants and dry-grass decals
/// (and never the snow flora).
/// </summary>
[TestOf(typeof(BiomeSystem))]
public sealed class WastelandBiomeTest : GameTest
{
    private static readonly ProtoId<BiomeTemplatePrototype> Wasteland = "F14Wasteland";

    [Test]
    public async Task SandPlantsAndGrass()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var tileDefs = server.ResolveDependency<ITileDefinitionManager>();
        var mapSys = entMan.System<SharedMapSystem>();
        MapId map = default;
        EntityUid mapUid = default;

        await server.WaitPost(() =>
        {
            mapUid = mapSys.CreateMap(out map);
            entMan.System<BiomeSystem>().EnsurePlanet(mapUid, protoMan.Index(Wasteland), 1234);
            var player = entMan.SpawnEntity("MobHuman", new MapCoordinates(0, 0, map));
            server.PlayerMan.SetAttachedEntity(server.PlayerMan.Sessions.Single(), player);
        });
        await Pair.RunSeconds(3f);

        await server.WaitAssertion(() =>
        {
            var grid = entMan.GetComponent<MapGridComponent>(mapUid);
            var sand = tileDefs["F14FloorWastelandSand"].TileId;
            for (var x = -20; x <= 20; x++)
            for (var y = -20; y <= 20; y++)
            {
                var tile = mapSys.GetTileRef(mapUid, grid, new Vector2i(x, y)).Tile;
                Assert.That(tile.TypeId, Is.EqualTo(sand), $"tile {x},{y} should be wasteland sand");
            }

            var plants = entMan.AllEntities<MetaDataComponent>().Count(e =>
                e.Comp.EntityPrototype?.ID.StartsWith("F14WastelandFloraWild") == true &&
                entMan.GetComponent<TransformComponent>(e).MapID == map);
            Assert.That(plants, Is.GreaterThan(0), "no wild Fallout plants generated");

            var decals = entMan.System<DecalSystem>()
                .GetDecalsIntersecting(mapUid, new Box2(-20, -20, 20, 20))
                .Select(d => d.Decal.Id).ToList();
            Assert.That(decals.Any(d => d.StartsWith("F14FloraDryGrass")), "no dry grass decals generated");
            Assert.That(decals.Any(d => d.StartsWith("F14FloraSnow")), Is.False, "snow flora on the desert");

            entMan.DeleteEntity(mapUid);
        });
    }
}
