#nullable enable
using Content.IntegrationTests.Fixtures;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Yuma loads the way the <c>mapping</c> command loads it (its own map id, uninitialized, paused).
/// </summary>
public sealed class YumaMappingLoadTest : GameTest
{
    [Test]
    public async Task LoadsForMapping()
    {
        var server = Pair.Server;
        var loader = server.EntMan.System<MapLoaderSystem>();
        var maps = server.EntMan.System<SharedMapSystem>();
        var mapId = new MapId(1000);

        await server.WaitAssertion(() =>
        {
            var opts = new DeserializationOptions { StoreYamlUids = true };
            Assert.That(loader.TryLoadMapWithId(mapId, new ResPath("/Maps/_WW/Yuma.yml"), out var map, out _, opts));
            Assert.That(maps.IsInitialized(mapId), Is.False, "mapping keeps it pre-init");
            Assert.That(maps.IsPaused(mapId));
            server.EntMan.DeleteEntity(map!.Value.Owner);
        });
    }
}
