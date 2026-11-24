#nullable enable
using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// 2026-11-22 sprite team barbed wire: both kinds join their own kind (S=1 E=2 N=4 W=8); loose wire can be waded
/// through (no hard fixture), the posted wire fence can't.
/// </summary>
public sealed class BarbedWireTest : InteractionTest
{
    [TestCase("WWBarbedWireLoose", "barbedwire_loose")]
    [TestCase("WWBarbedWireFence", "barbedwire_fence")]
    public async Task WireJoins(string proto, string baseState)
    {
        // Next to the target tile so the new tiles stay connected to the test grid.
        var grid = MapData.Grid.Owner;
        var target = SEntMan.System<SharedTransformSystem>().WithEntityId(ToServer(TargetCoords), grid);
        var corner = target.Offset(new System.Numerics.Vector2(1, 0));
        var east = target.Offset(new System.Numerics.Vector2(2, 0));
        var north = target.Offset(new System.Numerics.Vector2(1, 1));
        foreach (var c in new[] { corner, east, north })
            await SetTile(Plating, FromServer(c), MapData.Grid);

        EntityUid sCorner = default, sEast = default;
        await Server.WaitPost(() =>
        {
            sCorner = SEntMan.SpawnAtPosition(proto, corner);
            sEast = SEntMan.SpawnAtPosition(proto, east);
            SEntMan.SpawnAtPosition(proto, north);
        });
        await RunTicks(15);
        await Client.WaitPost(() => CEntMan.System<Content.Client.IconSmoothing.IconSmoothSystem>().FrameUpdate(0f));

        await Client.WaitAssertion(() =>
        {
            var sprites = CEntMan.System<SpriteSystem>();
            string State(EntityUid s)
            {
                var c = CEntMan.GetEntity(SEntMan.GetNetEntity(s));
                Assert.That(sprites.LayerMapTryGet(c, "direction", out var idx, false), "no smoothing layer");
                return sprites.LayerGetRsiState(c, idx).Name ?? "";
            }

            Assert.That(State(sCorner), Is.EqualTo($"{baseState}6"), "corner should join north and east");
            Assert.That(State(sEast), Is.EqualTo($"{baseState}8"), "east piece should join west");
        });

        await Server.WaitAssertion(() =>
        {
            var hard = SEntMan.GetComponent<FixturesComponent>(sCorner).Fixtures.Values.Any(f => f.Hard);
            Assert.That(hard, Is.EqualTo(proto == "WWBarbedWireFence"), $"{proto} hard fixture: {hard}");
        });
    }
}
