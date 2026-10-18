#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._WW.Maps;
using Content.Shared.Atmos.Components;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Maps converted with the planet script (scratchpad gen/make_planet.py) load as one planet: the map entity is the
/// ground, with the biome, air and PlanetFloor, and the build sits directly on it.
/// </summary>
public sealed class PlanetMapTest : GameTest
{
    [TestCase("/Maps/Test/dev_map_f14.yml")]
    public async Task LoadsAsPlanet(string path)
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var loader = entMan.System<MapLoaderSystem>();
        var mapSys = entMan.System<SharedMapSystem>();

        await server.WaitAssertion(() =>
        {
            Assert.That(loader.TryLoadMap(new ResPath(path), out var map, out var grids));
            var mapUid = map!.Value.Owner;
            Assert.That(entMan.HasComponent<MapGridComponent>(mapUid), "the map entity is the ground");
            Assert.That(entMan.HasComponent<BiomeComponent>(mapUid));
            Assert.That(entMan.HasComponent<MapAtmosphereComponent>(mapUid));
            Assert.That(entMan.HasComponent<PlanetFloorComponent>(mapUid));
            Assert.That(grids!.Select(g => g.Owner), Is.EquivalentTo(new[] { mapUid }), "no separate grids left");
            Assert.That(entMan.GetComponent<TransformComponent>(mapUid).ChildCount, Is.GreaterThan(100), "the build is on the map");
            entMan.DeleteEntity(mapUid);
        });
    }
}
